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
    private ObservableCollection<(ILogicallyCombinable<ICondition> Condition, ICombinationalBehavior Behavior)> ConditionMappings { get; } = [];

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
        // Check parent modules
        base.CheckTopLevelValidity(out exception);

        // Check that dimensions of all behaviors are compatible
        if (!Dimension.AreCompatible(ConditionMappings.Select(c => c.Behavior.Dimension)))
            exception = new Exception("Expressions are incompatible. Must have same or compatible dimensions");

        return exception is null;
    }

    /// <inheritdoc/>
    protected override SpiceCircuit GetSpiceWithoutCheck(INamedSignal outputSignal, string uniqueId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override int GetOutputValueWithoutCheck(RuleBasedSimulationState state, SignalReference outputSignal)
    {
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