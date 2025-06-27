using System.Collections.ObjectModel;
using System.Text;
using VHDLSharp.LogicTree;
using VHDLSharp.Utility;
using VHDLSharp.Conditions;
using VHDLSharp.Signals;
using VHDLSharp.Dimensions;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Simulations;
using System.Collections.Specialized;
using SpiceSharp.Entities;
using SpiceSharp.Components;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Behavior that uses sequential rather than combinational logic. 
/// Maps <see cref="Condition"/> objects to <see cref="ICombinationalBehavior"/> objects. 
/// The output signal is assigned the value from the combinational behavior when the condition is met. 
/// Priority is used for the conditions
/// </summary>
public class DynamicBehavior : Behavior
{
    /// <summary>
    /// Ordered mapping of condition to behavior
    /// </summary>
    public ObservableCollection<(ILogicallyCombinable<ICondition> Condition, ICombinationalBehavior Behavior)> ConditionMappings { get; } = [];

    /// <summary>
    /// Generate new dynamic behavior
    /// </summary>
    public DynamicBehavior()
    {
        ConditionMappings.CollectionChanged += ConditionMappingUpdated;
    }

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> NamedInputSignals => ConditionMappings.SelectMany(c => c.Behavior.NamedInputSignals.Union(c.Condition.BaseObjects.SelectMany(c => c.InputSignals).OfType<INamedSignal>())).Distinct();

    /// <inheritdoc/>
    public override Dimension Dimension => Dimension.CombineWithoutCheck(ConditionMappings.Select(c => c.Behavior.Dimension));

    /// <inheritdoc/>
    protected override string GetVhdlStatementWithoutCheck(INamedSignal outputSignal)
    {
        if (ConditionMappings.Count == 0)
            throw new Exception("Must have at least one condition mapping");
        
        StringBuilder sb = new();
        sb.AppendLine($"process({string.Join(", ", NamedInputSignals.Select(s => s.Name))}) is");
        sb.AppendLine("begin");

        // First condition
        (ILogicallyCombinable<ICondition> firstCondition, ICombinationalBehavior firstBehavior) = ConditionMappings.First();
        sb.AppendLine($"\tif ({firstCondition.ToLogicString()}) then");
        sb.AppendLine(firstBehavior.GetVhdlStatement(outputSignal).AddIndentation(2));

        // Remaining conditions
        foreach ((ILogicallyCombinable<ICondition> condition, ICombinationalBehavior behavior) in ConditionMappings.Skip(1))
        {
            sb.AppendLine($"\telse if ({condition.ToLogicString()}) then");
            sb.AppendLine(behavior.GetVhdlStatement(outputSignal).AddIndentation(2));
        }

        sb.AppendLine("\tend if;");
        sb.AppendLine("end process;");

        return sb.ToString();
    }

    /// <inheritdoc/>
    protected override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        // Check parent modules of input signals
        base.CheckTopLevelValidity(out exception);

        ILogicallyCombinable<ICondition>[] conditions = [.. ConditionMappings.Select(map => map.Condition)];
        if (conditions.Any(c => c.BaseObjects.Any(obj => obj is IEventDrivenCondition)))
        {
            // Check that there's only one event-driven condition
            if (conditions[0..^1].SelectMany(c => c.BaseObjects).OfType<IEventDrivenCondition>().Any())
                exception = new Exception("Only 1 event-driven condition is allowed, and it must be last");
            ILogicallyCombinable<ICondition> lastCondition = conditions[^1];
            if (lastCondition.BaseObjects.OfType<IEventDrivenCondition>().Count() > 1)
                exception = new Exception("Event-driven conditions cannot be chained together logically (e.g. Rising-Edge AND Falling-Edge)");

            // Event-driven condition must be at top level and can only be ANDed or ORed
            if (!(lastCondition is IEventDrivenCondition ||
                (lastCondition is And<ICondition> andCondition && andCondition.Inputs.Any(i => i is IEventDrivenCondition)) || 
                lastCondition is Or<ICondition> orCondition && orCondition.Inputs.Any(i => i is IEventDrivenCondition)))
                exception = new Exception("Event-driven condition must be standalone or ANDed/ORed with another condition");
        }

        // Check that dimensions of all behaviors are compatible
        if (!Dimension.AreCompatible(ConditionMappings.Select(c => c.Behavior.Dimension)))
            exception = new Exception("Expressions are incompatible. Must have same or compatible dimensions");

        return exception is null;
    }

    /// <inheritdoc/>
    protected override SpiceCircuit GetSpiceWithoutCheck(INamedSignal outputSignal, string uniqueId)
    {
        if (ConditionMappings.Count == 0)
            throw new Exception("Must have at least one condition mapping");

        // Create intermediate signals for inner conditions and behaviors, and add their circuits to list
        List<(INamedSignal value, ISingleNodeNamedSignal condition)> asyncSignals = [];
        INamedSignal? dSignal = null;
        ISingleNodeNamedSignal? clkSignal = null;
        ISingleNodeNamedSignal? enSignal = null;
        List<SpiceCircuit> intermediateCircuits = [];
        int dimension = outputSignal.Dimension.NonNullValue;
        foreach ((int i, (ILogicallyCombinable<ICondition> innerCondition, ICombinationalBehavior innerBehavior)) in ConditionMappings.Index())
        {
            // Generate intermediate signal from behavior matching dimension of output
            string intermediateName = SpiceUtil.GetSpiceName(uniqueId, 0, $"inner{i}");
            INamedSignal intSignal = dimension == 1 ? new Signal(intermediateName, outputSignal.ParentModule) :
                new Vector(intermediateName, outputSignal.ParentModule, outputSignal.Dimension.NonNullValue);
            intermediateCircuits.Add(innerBehavior.GetSpice(intSignal, $"{uniqueId}_{i}_0"));

            ILogicallyCombinable<ICondition> GetConditionWithoutEvent(LogicTree<ICondition> tree)
            {
                ILogicallyCombinable<ICondition>[] inputsWithoutEvent = [.. tree.Inputs.Where(i => i is not IEventDrivenCondition)];
                return inputsWithoutEvent.Length == 1 ? inputsWithoutEvent[0] : new And<ICondition>(inputsWithoutEvent);
            }

            void HandleEvent(IEventDrivenCondition eventDrivenCondition)
            {
                clkSignal = new Signal(SpiceUtil.GetSpiceName(uniqueId, 0, $"condition{i}Clk"), outputSignal.ParentModule);
                intermediateCircuits.Add(eventDrivenCondition.GetSpice($"{uniqueId}_{i}_0", clkSignal));
                dSignal = intSignal;
            }

            void HandleAsyncLoad(ILogicallyCombinable<ICondition> condition)
            {
                Signal select = new(SpiceUtil.GetSpiceName(uniqueId, 0, $"condition{i}Sel"), outputSignal.ParentModule);
                asyncSignals.Add((intSignal, select));
                // intermediateCircuits.Add(innerCondition.PerformFunction(select)); TODO
            }

            void HandleEnable(ILogicallyCombinable<ICondition> condition)
            {
                enSignal = new Signal(SpiceUtil.GetSpiceName(uniqueId, 0, $"condition{i}En"), outputSignal.ParentModule);
                // intermediateCircuits.Add(andWithoutEvent.PerformFunction(enSignal)) TODO
            }

            switch (innerCondition)
            {
                // Event-driven condition without enable
                case IEventDrivenCondition eventDrivenCondition:
                    HandleEvent(eventDrivenCondition);
                    break;
                // Event-driven condition with enable
                case And<ICondition> andCondition when andCondition.Inputs.OfType<IEventDrivenCondition>().FirstOrDefault() is IEventDrivenCondition eventDrivenCondition:
                    HandleEvent(eventDrivenCondition);
                    // Enable function
                    ILogicallyCombinable<ICondition> andWithoutEvent = GetConditionWithoutEvent(andCondition);
                    HandleEnable(andWithoutEvent);
                    break;
                // Event-driven condition without enable, paired with a constant condition
                case Or<ICondition> orCondition when orCondition.Inputs.OfType<IEventDrivenCondition>().FirstOrDefault() is IEventDrivenCondition eventDrivenCondition:
                    HandleEvent(eventDrivenCondition);
                    // Asynchronous load--Select
                    ILogicallyCombinable<ICondition> orWithoutEvent = GetConditionWithoutEvent(orCondition);
                    HandleAsyncLoad(orWithoutEvent);
                    break;
                // No event-driven condition
                default:
                    // Asynchronous load--Select
                    HandleAsyncLoad(innerCondition);
                    break;
            }
        }

        INamedSignal? dAfterEnSignal = dSignal;
        string qName = SpiceUtil.GetSpiceName(uniqueId, 0, $"Q");
        INamedSignal qSignal = dimension == 1 ? new Signal(qName, outputSignal.ParentModule) :
            new Vector(qName, outputSignal.ParentModule, outputSignal.Dimension.NonNullValue);
        List<IEntity> additionalEntities = [];
        string[]? dAfterEnSignalBits = null;
        string[] qSignalBits = [.. qSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];

        // MUX--add MUX in each dimension if enable signal is present
        if (enSignal is not null)
        {
            if (dSignal is null) throw new("Impossible");
            string dAfterEnName = SpiceUtil.GetSpiceName(uniqueId, 0, $"dAfterEn");
            dAfterEnSignal = dimension == 1 ? new Signal(dAfterEnName, outputSignal.ParentModule) :
                new Vector(dAfterEnName, outputSignal.ParentModule, outputSignal.Dimension.NonNullValue);
            string[] dSignalBits = [.. dSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
            dAfterEnSignalBits = [.. dAfterEnSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
            for (int i = 0; i < dimension; i++)
                additionalEntities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, i, "MUX"), SpiceUtil.GetMuxSubcircuit(1), dSignalBits[i], qSignalBits[i], dAfterEnSignalBits[i]));
        }

        dAfterEnSignalBits ??= dSignal is null ? [.. Enumerable.Range(0, dimension).Select(i => "0")] : [.. dSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];

        // DFF--add DFF in each dimension no matter what
        string[] outputSignalBits = [.. outputSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
        string[] asyncLoadSignalBits = []; // TODO
        for (int i = 0; i < dimension; i++)
            additionalEntities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, i, "DFF"), SpiceUtil.GetDffWithAsyncLoadSubcircuit(), dAfterEnSignalBits[i], outputSignalBits[i], clkSignal?.GetSpiceName() ?? "0", asyncLoadSignalBits[i], qSignalBits[i]));


        // for (int i = 0; i < outputSignal.Dimension.NonNullValue; i++)
        // {
        //     // MUX
        //     // ISingleNodeNamedSignal dSignal;
        //     if (enSignal is not null)
        //     {
        //         if (dSignal is null)
        //             throw new("Impossible");
        //         intermediateCircuits.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, i, "MUX"), SpiceUtil.GetMuxSubcircuit(1), ))
        //             }
        // }


        // // A single event-based condition
        // if (ConditionMappings.Count == 1 && ConditionMappings[0].Condition is IEventDrivenCondition)
        // {
        //     // Add DFF for each dimension
        //     foreach ((int i, (ISingleNodeNamedSignal outputSingleSignal, ISingleNodeNamedSignal interSingleSignal)) in outputSignal.ToSingleNodeSignals.Zip(intermediateSignals[0].ToSingleNodeSignals).Index())
        //     {
        //         additionalEntities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, i, "DFF"), SpiceUtil.GetDffWithAsyncLoadSubcircuit(), interSingleSignal.GetSpiceName(), "0", "", "0", outputSingleSignal.GetSpiceName()));
        //     }

        //     return new SpiceCircuit(additionalEntities).CombineWith(intermediateCircuits).WithCommonEntities();
        // }

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override int GetOutputValueWithoutCheck(RuleBasedSimulationState state, SignalReference outputSignal)
    {
        if (ConditionMappings.Count == 0)
            throw new Exception("Must have at least one condition mapping");

        // If first step, use 0 as value
        int lastIndex = state.CurrentTimeStepIndex - 1;
        if (lastIndex < 0)
            return 0;

        // Check if any condition set is satisfied--if so, use the corresponding value
        foreach ((ILogicallyCombinable<ICondition> Condition, ICombinationalBehavior Behavior) in ConditionMappings)
            if (EvaluateConditionCombo(Condition, state, outputSignal.Subcircuit))
                return Behavior.GetOutputValue(state, outputSignal);

        // Otherwise, use the previous value from the state
        return state.GetSignalValues(outputSignal)[lastIndex];
    }

    private bool EvaluateConditionCombo(ILogicallyCombinable<ICondition> conditionCombo, RuleBasedSimulationState state, SubcircuitReference context)
    {
        bool Primary(ICondition condition) => condition.Evaluate(state, context);
        bool And(IEnumerable<bool> inputs) => inputs.Aggregate((a, b) => a && b);
        bool Or(IEnumerable<bool> inputs) => inputs.Aggregate((a, b) => a || b);
        bool Not(bool input) => !input;
        return conditionCombo.PerformFunction(Primary, And, Or, Not);
    }

    private void ConditionMappingUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Track each new condition/behavior in validity manager
        if ((e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace) && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
                if (newItem is KeyValuePair<ILogicallyCombinable<ICondition>, ICombinationalBehavior> kvp)
                {
                    AddChildEntity(kvp.Key);
                    AddChildEntity(kvp.Value);
                }
        
        // If something has been removed, remove behavior from tracking
        if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset || e.Action == NotifyCollectionChangedAction.Replace) && e.OldItems is not null)
            foreach (object oldItem in e.OldItems)
                if (oldItem is KeyValuePair<ILogicallyCombinable<ICondition>, ICombinationalBehavior> kvp)
                {
                    RemoveChildEntity(kvp.Key);
                    RemoveChildEntity(kvp.Value);
                }

        // Invoke module update
        InvokeBehaviorUpdated(sender, e);
    }
}