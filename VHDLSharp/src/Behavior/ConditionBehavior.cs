using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Conditions;
using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Behavior defined by an ordered set of condition/behavior pairs. 
/// The behavior corresponding to the highest-priority true condition is applied. 
/// A default behavior can be defined as well. It is an output of 0 unless otherwise specified. 
/// All behaviors must be combination (implement <see cref="ICombinationalBehavior"/>)
/// </summary>
public class ConditionBehavior : Behavior, ICombinationalBehavior, IRecursiveBehavior
{
    private ICombinationalBehavior defaultBehavior = new ValueBehavior(0);

    /// <summary>
    /// Generate new condition behavior
    /// </summary>
    public ConditionBehavior()
    {
        ConditionMappings.CollectionChanged += ConditionMappingUpdated;
    }

    /// <summary>
    /// Default behavior to use when no condition is met. Originally set to output 0. 
    /// </summary>
    public ICombinationalBehavior DefaultBehavior
    {
        get => defaultBehavior;
        set
        {
            RemoveChildEntity(defaultBehavior);
            AddChildEntity(value);
            defaultBehavior = value;
            InvokeBehaviorUpdated(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Ordered mapping of condition to behavior
    /// </summary>
    public ObservableCollection<(ILogicallyCombinable<IConstantCondition> Condition, ICombinationalBehavior Behavior)> ConditionMappings { get; } = [];

    /// <inheritdoc/>
    public override HashSet<IModuleSpecificSignal> InputModuleSignals => GetInputModuleSignals();

    IEnumerable<IModuleSpecificSignal> IRecursiveBehavior.GetInputModuleSignals(ISet<IBehavior> behaviorsToIgnore) => GetInputModuleSignals(behaviorsToIgnore);

    // Special method to avoid recursion when getting input signals
    private HashSet<IModuleSpecificSignal> GetInputModuleSignals(ISet<IBehavior>? behaviorsToIgnore = null)
    {
        HashSet<IBehavior> childBehaviorsToIgnore = behaviorsToIgnore is null ? [this] : [.. behaviorsToIgnore, this];
        HashSet<IModuleSpecificSignal> signals = [];

        void HandleBehavior(ICombinationalBehavior behavior)
        {
            // Ignore all sub-behaviors that are in list to ignore
            // Includes itself (don't want immediate recursion) and sub-behaviors once they've been explored
            if (!childBehaviorsToIgnore.Contains(behavior))
            {
                IEnumerable<IModuleSpecificSignal> behaviorSignals = behavior is IRecursiveBehavior recursiveBehavior ? 
                    recursiveBehavior.GetInputModuleSignals(childBehaviorsToIgnore) : behavior.InputModuleSignals;
                foreach (IModuleSpecificSignal signal in behaviorSignals)
                    signals.Add(signal);
                childBehaviorsToIgnore.Add(behavior);
            }
        }

        foreach ((ILogicallyCombinable<IConstantCondition> condition, ICombinationalBehavior behavior) in ConditionMappings)
        {
            foreach (IModuleSpecificSignal signal in condition.BaseObjects.SelectMany(c => c.InputModuleSignals))
                signals.Add(signal);
            HandleBehavior(behavior);
        }
        HandleBehavior(DefaultBehavior);
        return signals;
    }

    /// <inheritdoc/>
    public override Dimension Dimension => GetDimension();

    Dimension IRecursiveBehavior.GetDimension(ISet<IBehavior> behaviorsToIgnore) => GetDimension(behaviorsToIgnore);

    private Dimension GetDimension(ISet<IBehavior>? behaviorsToIgnore = null)
    {
        HashSet<IBehavior> childBehaviorsToIgnore = behaviorsToIgnore is null ? [this] : [.. behaviorsToIgnore, this];
        List<Dimension> subDimensions = [];
        foreach (IBehavior behavior in ConditionMappings.Select(c => c.Behavior).Append(DefaultBehavior))
        {
            if (childBehaviorsToIgnore.Contains(behavior))
                continue;
            subDimensions.Add(behavior is IRecursiveBehavior recursiveBehavior ? 
                recursiveBehavior.GetDimension(childBehaviorsToIgnore) : behavior.Dimension);
            childBehaviorsToIgnore.Add(behavior);
        }
        return Dimension.CombineWithoutCheck(subDimensions);
    }

    IEnumerable<IMayBeRecursive<IRecursiveBehavior>> IMayBeRecursive<IRecursiveBehavior>.Children => ConditionMappings.Select(c => c.Behavior).Append(DefaultBehavior).OfType<IRecursiveBehavior>();

    /// <summary>
    /// Get or set behavior for a given condition.
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    public ICombinationalBehavior this[ILogicallyCombinable<IConstantCondition> condition]
    {
        get => ConditionMappings.First(c => c.Condition.Equals(condition)).Behavior;
        set
        {
            Remove(condition);
            ConditionMappings.Add((condition, value));
        }
    }

    /// <summary>
    /// Add a condition/behavior pair to the mappings list
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="behavior"></param>
    public void Add(ILogicallyCombinable<IConstantCondition> condition, ICombinationalBehavior behavior) => ConditionMappings.Add((condition, behavior));

    /// <summary>
    /// Remove mappings with a given condition
    /// </summary>
    /// <param name="condition"></param>
    public void Remove(ILogicallyCombinable<IConstantCondition> condition)
    {
        foreach (var pair in ConditionMappings.ToArray())
            if (pair.Condition.Equals(condition))
                ConditionMappings.Remove(pair);
    }

    /// <summary>
    /// Remove a condition/behavior pair from the mappings list
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="behavior"></param>
    public void Remove(ILogicallyCombinable<IConstantCondition> condition, ICombinationalBehavior behavior) => ConditionMappings.Remove((condition, behavior));

    /// <inheritdoc/>
    protected override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        // Check recursion
        if (((IRecursiveBehavior)this).CheckRecursion())
            exception = new IllegalRecursionException("Recursion detected in behavior. This is not allowed.");

        // Check parent modules of input signals
        base.CheckTopLevelValidity(out exception);

        // Check that dimensions of all behaviors are compatible
        if (!Dimension.AreCompatible(ConditionMappings.Select(c => c.Behavior.Dimension).Append(DefaultBehavior.Dimension)))
            exception = new Exception("Used behaviors are incompatible. Must have same or compatible dimensions");

        return exception is null;
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

    /// <inheritdoc/>
    protected override string GetVhdlStatementWithoutCheck(INamedSignal outputSignal)
    {
        if (ConditionMappings.Count == 0)
            return DefaultBehavior.GetVhdlStatement(outputSignal);

        StringBuilder sb = new();
        bool firstLoop = true;

        // Conditions
        foreach ((ILogicallyCombinable<IConstantCondition> condition, ICombinationalBehavior behavior) in ConditionMappings)
        {
            sb.AppendLine($"{(firstLoop ? "if" : "elsif")} ({condition.ToLogicString()}) then");
            sb.AppendLine(behavior.GetVhdlStatement(outputSignal).AddIndentation(1));
            firstLoop = false;
        }

        // Default
        sb.AppendLine("else");
        sb.AppendLine(defaultBehavior.GetVhdlStatement(outputSignal).AddIndentation(1));
        sb.AppendLine($"end if");

        return sb.ToString();
    }

    /// <inheritdoc/>
    protected override SpiceCircuit GetSpiceWithoutCheck(INamedSignal outputSignal, string uniqueId)
    {
        int conditionCount = ConditionMappings.Count;
        if (conditionCount == 0)
            return DefaultBehavior.GetSpice(outputSignal, uniqueId);

        IModule module = outputSignal.ParentModule;
        int dimension = outputSignal.Dimension.NonNullValue;
        List<SpiceCircuit> intermediateCircuits = [];
        List<IEntity> additionalEntities = [];

        List<(INamedSignal value, ISingleNodeNamedSignal condition)> signalPairs = [];
        // Go through condition/behavior pairs
        int i = 0;
        foreach ((ILogicallyCombinable<IConstantCondition> innerCondition, ICombinationalBehavior innerBehavior) in ConditionMappings)
        {
            int j = 0;
            // Generate intermediate signal from behavior matching dimension of output
            string intermediateName = SpiceUtil.GetSpiceName(uniqueId, 0, $"inner{i}");
            INamedSignal intSignal = NamedSignal.GenerateSignalOrVector(intermediateName, module, dimension);
            intermediateCircuits.Add(innerBehavior.GetSpice(intSignal, $"{uniqueId}_{i}_{j++}"));

            Signal enSignal = new(SpiceUtil.GetSpiceName(uniqueId, 0, $"condition{i}"), module);
            intermediateCircuits.Add(innerCondition.ToBasicCondition().GenerateLogicalObject(IConstantCondition.ConditionSpiceSharpObjectOptions, new()
            {
                UniqueId = $"{uniqueId}_{i}_{j++}",
                OutputSignal = enSignal,
            }));

            signalPairs.Add((intSignal, enSignal));
            i++;
        }

        // Default behavior
        string defaultIntermediateName = SpiceUtil.GetSpiceName(uniqueId, 0, "default");
        INamedSignal defaultBehaviorSignal = NamedSignal.GenerateSignalOrVector(defaultIntermediateName, module, dimension);
        intermediateCircuits.Add(defaultBehavior.GetSpice(defaultBehaviorSignal, $"{uniqueId}_{i}"));

        // MUX tree
        INamedSignal muxOut = outputSignal;
        foreach ((int j, (INamedSignal value, ISingleNodeNamedSignal condition)) in signalPairs.Index())
        {
            INamedSignal zeroInput = j == conditionCount - 1 ? defaultBehaviorSignal :
                NamedSignal.GenerateSignalOrVector(SpiceUtil.GetSpiceName(uniqueId, 0, $"MUXOut{j}"), module, dimension);
            string[] zeroInputBits = [.. zeroInput.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
            string[] oneInputBits = [.. value.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
            string[] outBits = [.. outputSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
            for (int k = 0; k < zeroInputBits.Length; k++)
                additionalEntities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, k, $"MUX{j}"), SpiceUtil.GetMuxSubcircuit(1), condition.GetSpiceName(), zeroInputBits[k], oneInputBits[k], outBits[k]));
            muxOut = zeroInput;
        }

        return SpiceCircuit.Combine([.. intermediateCircuits, new(additionalEntities)]);
    }

    /// <inheritdoc/>
    protected override int GetOutputValueWithoutCheck(RuleBasedSimulationState state, SignalReference outputSignal)
    {
        // Get output from behavior with highest-priority true condition, or default
        foreach ((ILogicallyCombinable<IConstantCondition> condition, ICombinationalBehavior behavior) in ConditionMappings)
            if (Util.EvaluateConditionCombo(condition.ToBasicCondition(), state, outputSignal.Subcircuit))
                return behavior.GetOutputValue(state, outputSignal);
        return DefaultBehavior.GetOutputValue(state, outputSignal);
    }
}