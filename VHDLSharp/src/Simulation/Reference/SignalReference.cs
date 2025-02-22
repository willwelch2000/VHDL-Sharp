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
    public SignalReference(SubcircuitReference subcircuitReference, INamedSignal signal)
    {
        Subcircuit = subcircuitReference;
        Signal = signal;
        CheckValid();

        // Check valid whenever module is updated
        TopLevelModule.Updated += (sender, e) => CheckValid();
    }

    /// <summary>
    /// Reference to subcircuit that contains this signal
    /// </summary>
    public SubcircuitReference Subcircuit { get; }

    /// <summary>
    /// Signal being referenced--must be in <see cref="Subcircuit"/>
    /// </summary>
    public INamedSignal Signal { get; }

    /// <inheritdoc/>
    public IModule TopLevelModule => Subcircuit.TopLevelModule;

    /// <inheritdoc/>
    public ReadOnlyCollection<IInstantiation> Path => Subcircuit.Path;

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

    /// <summary>
    /// Returns true if two references are equal
    /// </summary>
    /// <param name="reference1"></param>
    /// <param name="reference2"></param>
    /// <returns></returns>
    public static bool operator==(SignalReference reference1, SignalReference reference2) => reference1.Equals(reference2);
    
    /// <summary>
    /// Returns true if two references are not equal
    /// </summary>
    /// <param name="reference1"></param>
    /// <param name="reference2"></param>
    /// <returns></returns>
    public static bool operator!=(SignalReference reference1, SignalReference reference2) => !reference1.Equals(reference2);
    
    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Subcircuit, Signal);

    /// <summary>
    /// Get references to this signal that can be used in a Spice# simulation
    /// </summary>
    public IEnumerable<Reference> GetSpiceSharpReferences()
    {
        foreach (ISingleNodeNamedSignal singleNodeSignal in Signal.ToSingleNodeSignals)
            yield return new([.. Subcircuit.Path.Select(i => i.SpiceName), singleNodeSignal.GetSpiceName()]);
    }

    internal void CheckValid()
    {
        // Exception if last module doesn't contain signal
        IModule lastModule = Subcircuit.FinalModule;
        if (!lastModule.ContainsSignal(Signal))
            throw new SubcircuitPathException($"Module {lastModule} does not contain given signal ({Signal})");
    }
}