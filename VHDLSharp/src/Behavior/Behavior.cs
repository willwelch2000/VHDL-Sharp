using VHDLSharp.Dimensions;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Validation;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Simulations;
using VHDLSharp.Exceptions;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Abstract class defining a behavior that makes up a module
/// </summary>
public abstract class Behavior : IBehavior, IValidityManagedEntity
{
    private EventHandler? updated;

    private readonly ValidityManager validityManager;
    
    // TODO for now, this does not follow the modules it uses because its validity doesn't depend on it.
    // It also doesn't track the signals, because they are not supposed to change their parent module
    private readonly ObservableCollection<object> childEntities;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Behavior()
    {
        childEntities = [];
        validityManager = new ValidityManager<object>(this, childEntities);
    }
    
    /// <summary>
    /// Get all of the named input signals used in this behavior
    /// </summary>
    public abstract IEnumerable<INamedSignal> NamedInputSignals { get; }

    /// <summary>
    /// Dimension of behavior, as a <see cref="Dimension"/> object
    /// </summary>
    public abstract Dimension Dimension { get; }

    /// <summary>
    /// Event called when a property of the behavior is changed that could affect other objects
    /// </summary>
    public event EventHandler? Updated
    {
        add
        {
            updated -= value; // remove if already present
            updated += value;
        }
        remove => updated -= value;
    }

    /// <summary>
    /// Module this behavior refers to, found from the signals
    /// Null if no input signals, meaning that it has no specific module
    /// </summary>
    public IModule? ParentModule => NamedInputSignals.FirstOrDefault()?.ParentModule;

    /// <summary>
    /// Checks that the behavior is valid given the input signals. 
    /// Base version just checks that all input signals come from the same module
    /// </summary>
    /// <exception cref="Exception"></exception>
    protected virtual bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;
        var modules = NamedInputSignals.Select(s => s.ParentModule).Distinct();
        if (modules.Count() > 1)
            exception = new Exception("Input signals should all come from the same module");
        return exception is null;
    }

    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception) => CheckTopLevelValidity(out exception);

    /// <inheritdoc/>
    public ValidityManager ValidityManager => validityManager;

    /// <inheritdoc/>
    public string GetVhdlStatement(INamedSignal outputSignal)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Behavior must be valid to convert to VHDL");
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return GetVhdlStatementWithoutCheck(outputSignal);
    }
    
    /// <inheritdoc/>
    public SpiceCircuit GetSpice(INamedSignal outputSignal, string uniqueId)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Behavior must be valid to convert to Spice circuit");
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return GetSpiceWithoutCheck(outputSignal, uniqueId);
    }
    
    /// <inheritdoc/>
    public SimulationRule GetSimulationRule(SignalReference outputSignal)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Logic behavior must be valid to convert to Spice circuit");
        if (!((IValidityManagedEntity)outputSignal).ValidityManager.IsValid())
            throw new InvalidException("Output signal must be valid to use to get simulation rule");
        if (!IsCompatible(outputSignal.Signal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return new(outputSignal, (state) => GetOutputValueWithoutCheck(state, outputSignal));
    }

    /// <summary>
    /// Get VHDL representation given the assigned output signal without checking validity. 
    /// The validity check is managed by the base class
    /// </summary>
    protected abstract string GetVhdlStatementWithoutCheck(INamedSignal outputSignal);
    
    /// <summary>
    /// Get Spice circuit object without checking validity. 
    /// The validity check is managed by the base class
    /// </summary>
    /// <param name="outputSignal">Output signal for this behavior</param>
    /// <param name="uniqueId">Unique string provided to this behavior so that it can have a unique name</param>
    /// <returns></returns>
    protected abstract SpiceCircuit GetSpiceWithoutCheck(INamedSignal outputSignal, string uniqueId);

    /// <inheritdoc/>
    public int GetOutputValue(RuleBasedSimulationState state, SignalReference outputSignal)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Logic behavior must be valid to convert to Spice circuit");
        if (!((IValidityManagedEntity)outputSignal).ValidityManager.IsValid())
            throw new InvalidException("Output signal must be valid to use to get output value");
        if (!IsCompatible(outputSignal.Signal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return GetOutputValueWithoutCheck(state, outputSignal);
    }
    
    /// <summary>
    /// Get output value given simulation state and subcircuit context. 
    /// Validity check has already been performed when this is called
    /// </summary>
    /// <param name="state">Current state of the simulation</param>
    /// <param name="outputSignal">Reference to output signal--subcircuit can be used as context</param>
    /// <returns></returns>
    protected abstract int GetOutputValueWithoutCheck(RuleBasedSimulationState state, SignalReference outputSignal);
    
    /// <summary>
    /// Call this method to raise the <see cref="Updated"/> event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void InvokeBehaviorUpdated(object? sender, EventArgs e) => updated?.Invoke(sender, e);

    /// <summary>
    /// Check that a given output signal is compatible with this
    /// </summary>
    /// <param name="outputSignal"></param>
    public bool IsCompatible(INamedSignal outputSignal)
    {
        if (ParentModule is not null && outputSignal.ParentModule != ParentModule)
            return false;
        return Dimension.Compatible(outputSignal.Dimension);
    }

    /// <summary>
    /// Add a tracked child entity
    /// </summary>
    /// <param name="entity"></param>
    protected void AddChildEntity(object entity) => childEntities.Add(entity);

    /// <summary>
    /// Remove a tracked child entity
    /// </summary>
    /// <param name="entity"></param>
    protected void RemoveChildEntity(object entity) => childEntities.Remove(entity);
}