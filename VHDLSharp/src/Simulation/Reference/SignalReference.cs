using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using SpiceSharp.Simulations.Base;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Validation;

namespace VHDLSharp.Simulations;

/// <summary>
/// Reference to a single-node named signal in the circuit hierarchy
/// </summary>
public class SignalReference : IEquatable<SignalReference>, ICircuitReference, IValidityManagedEntity
{
    private EventHandler? updated;

    private readonly ValidityManager manager;

    /// <summary>
    /// Create signal reference given subcircuit reference and signal in that subcircuit
    /// </summary>
    /// <param name="subcircuitReference"></param>
    /// <param name="signal"></param>
    public SignalReference(SubcircuitReference subcircuitReference, INamedSignal signal)
    {
        Subcircuit = subcircuitReference;
        Signal = signal;
        manager = new ValidityManager<SubcircuitReference>(this, [subcircuitReference]);
        // Call updated to check after construction
        updated?.Invoke(this, EventArgs.Empty);
        if (!((IValidityManagedEntity)this).CheckTopLevelValidity(out Exception? exception))
            throw exception;
    }

    ValidityManager IValidityManagedEntity.ValidityManager => manager;
    
    event EventHandler? IValidityManagedEntity.Updated
    {
        add
        {
            updated -= value; // remove if already present
            updated += value;
        }
        remove => updated -= value;
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
        if (!manager.IsValid())
            throw new InvalidException("Signal reference is invalid");
        foreach (ISingleNodeNamedSignal singleNodeSignal in Signal.ToSingleNodeSignals)
            yield return new([.. Subcircuit.Path.Select(i => i.SpiceName), singleNodeSignal.GetSpiceName()]);
    }

    /// <summary>
    /// If this signal reference is another name for a higher-level signal (connected via module port),
    /// get that signal reference instead
    /// </summary>
    /// <returns></returns>
    public SignalReference Ascend()
    {
        // No higher-level module
        if (Path.Count == 0)
            return this;
            
        IInstantiation lastInstantiation = Path.Last();
        SubcircuitReference ascendedSubcircuit = new(TopLevelModule, Path.SkipLast(1));

        // Test if this signal is part of the port mapping of the last instance
        // If so, go to that connection and continue to ascend from there
        if (Signal.IsPartOfPortMapping(lastInstantiation.PortMapping, out INamedSignal? connection))
        {
            SignalReference singleAscend = new(ascendedSubcircuit, connection);
            return singleAscend.Ascend();
        }
        
        // This isn't port
        return this;
    }

    /// <summary>
    /// Get this reference as all of its single-node signal references
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SignalReference> GetSingleNodeReferences()
    {
        foreach (ISingleNodeNamedSignal singleNodeSignal in Signal.ToSingleNodeSignals)
            yield return Subcircuit.GetChildSignalReference(singleNodeSignal);
    }

    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;
        // Problem if last module doesn't contain signal
        IModule lastModule = Subcircuit.FinalModule;
        if (Signal.ParentModule != lastModule)
            exception = new SubcircuitPathException($"Module {lastModule} does not contain given signal ({Signal})");
        return exception is null;
    }
}