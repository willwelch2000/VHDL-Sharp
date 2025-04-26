using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;

namespace VHDLSharp.Conditions;

/// <summary>
/// Condition that can be used in a dynamic behavior
/// </summary>
public abstract class Condition : ICondition
{
    /// <inheritdoc/>
    public IEnumerable<ICondition> BaseObjects => [this];

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ICondition> other)
    {
        ICondition? otherCondition = other.BaseObjects.FirstOrDefault(c => c.ParentModule is not null);
        return otherCondition is null || otherCondition.ParentModule == ParentModule;
    }

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <inheritdoc/>
    // public abstract bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context);
    public bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context) => throw new NotImplementedException();

    /// <summary>
    /// Get parent module based on named input signals
    /// </summary>
    public IModule? ParentModule => (InputSignals.FirstOrDefault(s => s is INamedSignal) as INamedSignal)?.ParentModule;

    /// <summary>
    /// Input signals to condition
    /// </summary>
    public abstract IEnumerable<ISignal> InputSignals { get; }
}