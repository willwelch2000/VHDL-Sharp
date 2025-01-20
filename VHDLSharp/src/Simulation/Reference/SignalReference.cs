using System.Collections.ObjectModel;
using SpiceSharp.Simulations.Base;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Reference to a single-node named signal in the circuit hierarchy
/// </summary>
public class SignalReference : IEquatable<SignalReference>, ICircuitReference
{
    /// <summary>
    /// Create signal reference given subcircuit reference and signal in that subcircuit
    /// </summary>
    /// <param name="subcircuitReference"></param>
    /// <param name="signal"></param>
    public SignalReference(SubcircuitReference subcircuitReference, SingleNodeNamedSignal signal)
    {
        Subcircuit = subcircuitReference;
        Signal = signal;
        CheckValid();

        // Check valid whenever module is updated
        TopLevelModule.ModuleUpdated += (object? sender, EventArgs e) => CheckValid();
    }

    /// <summary>
    /// Reference to subcircuit that contains this signal
    /// </summary>
    public SubcircuitReference Subcircuit { get; }

    /// <summary>
    /// Signal being referenced--must be in <see cref="Subcircuit"/>
    /// </summary>
    public SingleNodeNamedSignal Signal { get; }

    /// <inheritdoc/>
    public Module TopLevelModule => Subcircuit.TopLevelModule;

    /// <inheritdoc/>
    public ReadOnlyCollection<Instantiation> Path => Subcircuit.Path;

    /// <summary>
    /// Compare to another signal reference
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SignalReference? other)
    {
        return other is not null && Subcircuit.Equals(other.Subcircuit) && Signal == other.Signal;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SignalReference signalReference && Equals(signalReference);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Subcircuit, Signal);

    /// <summary>
    /// Get reference to this signal that can be used in a Spice# simulation
    /// </summary>
    public Reference GetSpiceSharpReference() => new([.. Subcircuit.Path.Select(i => i.SpiceName), Signal.ToSpice()]);

    internal void CheckValid()
    {
        // TODO I don't think this needs to do subcircuit checkValid bc that manages itself
        // Subcircuit.CheckValid();

        // Exception if last module doesn't contain signal
        Module lastModule = Subcircuit.FinalModule;
        if (!lastModule.NamedSignals.SelectMany(s => s.ToSingleNodeNamedSignals).Contains(Signal))
            throw new SubcircuitPathException($"Module {lastModule} does not contain given signal ({Signal})");
    }
}