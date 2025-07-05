using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;
using System.Diagnostics.CodeAnalysis;

namespace VHDLSharp.Conditions;

/// <summary>
/// Condition that can be used in a dynamic behavior
/// </summary>
public abstract class Condition : ICondition, IValidityManagedEntity
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public Condition()
    {
        // No child entities because this assumes the signals don't change their parent modules
        ValidityManager = new ValidityManager<object>(this, []);
    }

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
    public abstract bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context);

    /// <summary>
    /// Get parent module based on named input signals
    /// </summary>
    public IModule? ParentModule => (InputSignals.FirstOrDefault(s => s is INamedSignal) as INamedSignal)?.ParentModule;

    /// <summary>
    /// Input signals to condition
    /// </summary>
    public abstract IEnumerable<ISignal> InputSignals { get; }

    /// <inheritdoc/>
    public virtual ValidityManager ValidityManager { get; }

    /// <inheritdoc/>
    public virtual bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        if (InputSignals.OfType<INamedSignal>().Select(s => s.ParentModule).Distinct().Count() > 1)
        {
            exception = new Exception("Input signals come from multiple modules");
            return false;
        }
        exception = null;
        return true;
    }
}