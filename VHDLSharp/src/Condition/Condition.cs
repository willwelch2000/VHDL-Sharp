using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;

namespace VHDLSharp.Conditions;

/// <summary>
/// Condition that can be used in a dynamic behavior
/// </summary>
public abstract class Condition : ICondition, IValidityManagedEntity
{
    private readonly ObservableCollection<object> childEntities;
    
    /// <summary>
    /// Default constructor
    /// </summary>
    public Condition()
    {
        // Only child entities are derived signals because this assumes the signals don't change their parent modules
        childEntities = [];
        ValidityManager = new ValidityManager<object>(this, childEntities);
    }

    /// <inheritdoc/>
    public IEnumerable<ICondition> BaseObjects => [this];

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ICondition> other)
    {
        ICondition? otherCondition = other.BaseObjects.FirstOrDefault(c => c.ParentModule is not null);
        return otherCondition is null || (otherCondition.ParentModule?.Equals(ParentModule) ?? false);
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

    /// <summary>
    /// Should be called by child classes whenever new signals are added. 
    /// Finds the derived signals and tracks them
    /// </summary>
    /// <param name="newSignals"></param>
    protected void ManageNewSignals(IEnumerable<ISignal> newSignals)
    {
        foreach (IDerivedSignal derivedSignal in newSignals.OfType<IDerivedSignal>().Concat(newSignals.OfType<IDerivedSignalNode>().Select(s => s.DerivedSignal)))
            childEntities.Add(derivedSignal);
    }
}