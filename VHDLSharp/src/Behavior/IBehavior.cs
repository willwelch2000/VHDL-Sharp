using VHDLSharp.Dimensions;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Simulations;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Interface defining a behavior that can make up a module
/// </summary>
public interface IBehavior
{
    /// <summary>
    /// Get all of the module-specific input signals used in this behavior.
    /// Derived signals are not "unpacked" to get their used signals
    /// </summary>
    public IEnumerable<IModuleSpecificSignal> InputModuleSignals { get; }

    /// <summary>
    /// Dimension of behavior, as a <see cref="Dimension"/> object
    /// </summary>
    public Dimension Dimension { get; }

    /// <summary>
    /// Module this behavior refers to, found from the signals
    /// Null if no input signals, meaning that it has no specific module
    /// </summary>
    public IModule? ParentModule { get; }

    /// <summary>
    /// Get VHDL representation given the assigned output signal
    /// </summary>
    public string GetVhdlStatement(INamedSignal outputSignal);

    /// <summary>
    /// Get Spice circuit object
    /// </summary>
    /// <param name="outputSignal">Output signal for this behavior</param>
    /// <param name="uniqueId">Unique string provided to this behavior so that it can have a unique name</param>
    /// <returns></returns>
    public SpiceCircuit GetSpice(INamedSignal outputSignal, string uniqueId);

    /// <summary>
    /// Get simulation rule for a specific signal
    /// </summary>
    /// <param name="outputSignal">Specific output signal for this behavior</param>
    /// <returns></returns>
    public SimulationRule GetSimulationRule(SignalReference outputSignal);

    /// <summary>
    /// Get output value given simulation state and subcircuit context. 
    /// </summary>
    /// <param name="state">Current state of the simulation</param>
    /// <param name="outputSignal">Reference to output signal--subcircuit can be used as context</param>
    /// <returns></returns>
    public int GetOutputValue(RuleBasedSimulationState state, SignalReference outputSignal);

    /// <summary>
    /// Check that a given output signal is compatible with this
    /// </summary>
    /// <param name="outputSignal"></param>
    public bool IsCompatible(INamedSignal outputSignal);
}