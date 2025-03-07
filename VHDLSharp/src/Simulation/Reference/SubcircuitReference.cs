using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Validation;

namespace VHDLSharp.Simulations;

/// <summary>
/// Reference to a subcircuit instantiation in the circuit hierarchy
/// </summary>
public class SubcircuitReference : IEquatable<SubcircuitReference>, ICircuitReference, IValidityManagedEntity
{
    private EventHandler? updated;

    private readonly ValidityManager manager;

    /// <summary>
    /// Create subcircuit reference given top-level module and path to subcircuit
    /// </summary>
    /// <param name="topLevelModule"></param>
    /// <param name="path"></param>
    public SubcircuitReference(IModule topLevelModule, IEnumerable<IInstantiation> path)
    {
        TopLevelModule = topLevelModule;
        Path = new([.. path]);
        manager = new ValidityManager<object>(this, [topLevelModule, .. path]);
        // Call updated to check after construction
        updated?.Invoke(this, EventArgs.Empty);
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

    /// <inheritdoc/>
    public IModule TopLevelModule { get; }

    /// <inheritdoc/>
    public ReadOnlyCollection<IInstantiation> Path { get; }

    /// <summary>
    /// Final module in path
    /// </summary>
    public IModule FinalModule => Path.Select(i => i.InstantiatedModule).LastOrDefault(TopLevelModule);

    /// <summary>
    /// Gets subcircuit or signal reference inside this subcircuit
    /// If there are names of both, the subcircuit is returned
    /// </summary>
    /// <param name="name">Name of signal or subcircuit</param>
    /// <returns></returns>
    public ICircuitReference this[string name]
    {
        get
        {
            // Try to find instantiation
            IInstantiation? instantiation = FinalModule.Instantiations.FirstOrDefault(i => i.Name == name);
            if (instantiation is not null)
                return new SubcircuitReference(TopLevelModule, [.. Path, instantiation]);

            // Try to find signal
            ISingleNodeNamedSignal? signal = FinalModule.NamedSignals.SelectMany(s => s.ToSingleNodeSignals).FirstOrDefault(s => s.Name == name);
            if (signal is not null)
                return new SignalReference(this, signal);

            throw new SubcircuitPathException($"Could not find {name} in module {FinalModule}");
        }
    }

    /// <summary>
    /// Get child subcircuit reference given instantiation
    /// </summary>
    /// <param name="instantiation"></param>
    /// <returns></returns>
    public SubcircuitReference GetChildSubcircuitReference(IInstantiation instantiation) => new(TopLevelModule, [.. Path, instantiation]);

    /// <summary>
    /// Try to get child subcircuit reference given instantiation name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="reference">Output reference</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool TryGetChildSubcircuitReference(string name, out SubcircuitReference? reference)
    {
        // Try to find instantiation
        IInstantiation? instantiation = FinalModule.Instantiations.FirstOrDefault(i => i.Name == name);
        if (instantiation is not null)
        {
            reference = new SubcircuitReference(TopLevelModule, [.. Path, instantiation]);
            return true;
        }

        reference = null;
        return false;
    }

    /// <summary>
    /// Get child subcircuit reference given instantiation name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="SubcircuitPathException">Thrown if not found</exception>
    public SubcircuitReference GetChildSubcircuitReference(string name) =>
        TryGetChildSubcircuitReference(name, out SubcircuitReference? reference) && reference is not null ? reference : throw new SubcircuitPathException($"Could not find instantiation {name} in module {FinalModule}");

    /// <summary>
    /// Get child signal reference given signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public SignalReference GetChildSignalReference(ISingleNodeNamedSignal signal) => new(this, signal);

    /// <summary>
    /// Try to get child signal reference given signal name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="reference">Output reference</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool TryGetChildSignalReference(string name, out SignalReference? reference)
    {
        // Try to find signal
        ISingleNodeNamedSignal? signal = FinalModule.NamedSignals.SelectMany(s => s.ToSingleNodeSignals).FirstOrDefault(s => s.Name == name);
        if (signal is not null)
        {
            reference = new SignalReference(this, signal);
            return true;
        }

        reference = null;
        return false;
    }

    /// <summary>
    /// Get child signal reference given signal name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="SubcircuitPathException">Thrown if not found</exception>
    public SignalReference GetChildSignalReference(string name) =>
        TryGetChildSignalReference(name, out SignalReference? reference) && reference is not null ? reference : throw new SubcircuitPathException($"Could not find signal {name} in module {FinalModule}");

    /// <summary>
    /// Compare to another subcircuit reference
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SubcircuitReference? other)
    {
        return other is not null && TopLevelModule == other.TopLevelModule && Path.SequenceEqual(other.Path);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SubcircuitReference subcircuit && Equals(subcircuit);

    /// <summary>
    /// Returns true if two references are equal
    /// </summary>
    /// <param name="reference1"></param>
    /// <param name="reference2"></param>
    /// <returns></returns>
    public static bool operator==(SubcircuitReference reference1, SubcircuitReference reference2) => reference1.Equals(reference2);
    
    /// <summary>
    /// Returns true if two references are not equal
    /// </summary>
    /// <param name="reference1"></param>
    /// <param name="reference2"></param>
    /// <returns></returns>
    public static bool operator!=(SubcircuitReference reference1, SubcircuitReference reference2) => !reference1.Equals(reference2);
    
    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(TopLevelModule);
        foreach (IInstantiation instantiation in Path)
            hash.Add(instantiation);
        return hash.ToHashCode();
    }

    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)]out string explanation)
    {
        explanation = null;
        IModule module = TopLevelModule;
        foreach (IInstantiation instantiation in Path)
        {
            if (instantiation.ParentModule != module)
                explanation = $"Parent module of instantiation ({instantiation}) doesn't match {module}";
            if (!module.Instantiations.Contains(instantiation))
                explanation = $"Module {module} does not contain given instantiation ({instantiation})";
            module = instantiation.InstantiatedModule;
        }
        return explanation is null;
    }
}