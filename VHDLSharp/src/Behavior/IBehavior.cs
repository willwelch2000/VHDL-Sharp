using VHDLSharp.Dimensions;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using SpiceSharp.Entities;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Interface defining a behavior that can make up a module
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Get all of the named input signals used in this behavior
    /// </summary>
    public IEnumerable<INamedSignal> NamedInputSignals { get; }

    /// <summary>
    /// Get VHDL representation given the assigned output signal
    /// </summary>
    public string GetVhdlStatement(INamedSignal outputSignal);

    /// <summary>
    /// Dimension of behavior, as a <see cref="Dimension"/> object
    /// </summary>
    public Dimension Dimension { get; }

    /// <summary>
    /// Event called when a property of the behavior is changed that could affect other objects
    /// </summary>
    public event EventHandler? BehaviorUpdated;

    /// <summary>
    /// Module this behavior refers to, found from the signals
    /// Null if no input signals, meaning that it has no specific module
    /// </summary>
    public Module? ParentModule { get; }

    /// <summary>
    /// Convert to Spice
    /// </summary>
    /// <param name="outputSignal">Output signal for this behavior</param>
    /// <param name="uniqueId">Unique string provided to this instantiation so that it can have a unique name</param>
    /// <returns></returns>
    public string GetSpice(INamedSignal outputSignal, string uniqueId);

    /// <summary>
    /// Get behavior as list of entities for Spice#
    /// </summary>
    /// <param name="outputSignal">Output signal for this behavior</param>
    /// <param name="uniqueId">Unique string provided to this behavior so that it can have a unique name</param>
    /// <returns></returns>
    public IEnumerable<IEntity> GetSpiceSharpEntities(INamedSignal outputSignal, string uniqueId);

    /// <summary>
    /// Check that a given output signal is compatible with this
    /// </summary>
    /// <param name="outputSignal"></param>
    public bool IsCompatible(INamedSignal outputSignal);
}