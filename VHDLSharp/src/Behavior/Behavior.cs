using VHDLSharp.Dimensions;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using SpiceSharp.Entities;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Abstract class defining a behavior that makes up a module
/// </summary>
public abstract class Behavior : IBehavior
{
    private EventHandler? behaviorUpdated;

    /// <summary>
    /// Get all of the named input signals used in this behavior
    /// </summary>
    public abstract IEnumerable<INamedSignal> NamedInputSignals { get; }

    /// <summary>
    /// Get VHDL representation given the assigned output signal
    /// </summary>
    public abstract string GetVhdlStatement(INamedSignal outputSignal);

    /// <summary>
    /// Dimension of behavior, as a <see cref="Dimension"/> object
    /// </summary>
    public abstract Dimension Dimension { get; }

    /// <summary>
    /// Event called when a property of the behavior is changed that could affect other objects
    /// </summary>
    public event EventHandler? BehaviorUpdated
    {
        add
        {
            behaviorUpdated -= value; // remove if already present
            behaviorUpdated += value;
        }
        remove => behaviorUpdated -= value;
    }

    /// <summary>
    /// Module this behavior refers to, found from the signals
    /// Null if no input signals, meaning that it has no specific module
    /// </summary>
    public IModule? ParentModule => NamedInputSignals.FirstOrDefault()?.ParentModule;

    /// <summary>
    /// Checks that the behavior is valid given the input signals. 
    /// Base version just checks that all input signals come from the same module. 
    /// This should be wrapped in a try-catch so that whatever causes the problem can be undone
    /// </summary>
    /// <exception cref="Exception"></exception>
    protected virtual void CheckValid()
    {
        var modules = NamedInputSignals.Select(s => s.ParentModule).Distinct();
        if (modules.Count() > 1)
            throw new Exception("Input signals should all come from the same module");
    }

    /// <summary>
    /// Convert to spice
    /// </summary>
    /// <param name="outputSignal">Output signal for this behavior</param>
    /// <param name="uniqueId">Unique string provided to this instantiation so that it can have a unique name</param>
    /// <returns></returns>
    public abstract string GetSpice(INamedSignal outputSignal, string uniqueId);

    /// <summary>
    /// Get behavior as list of entities for Spice#
    /// </summary>
    /// <param name="outputSignal">Output signal for this behavior</param>
    /// <param name="uniqueId">Unique string provided to this behavior so that it can have a unique name</param>
    /// <returns></returns>
    public abstract IEnumerable<IEntity> GetSpiceSharpEntities(INamedSignal outputSignal, string uniqueId);

    /// <summary>
    /// Call this method to raise the <see cref="BehaviorUpdated"/> event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void RaiseBehaviorChanged(object? sender, EventArgs e) => behaviorUpdated?.Invoke(sender, e);

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
}