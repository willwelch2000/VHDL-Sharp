using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Validation;

namespace VHDLSharp.Simulations;

/// <summary>
/// Reference to a module instantiation in the circuit hierarchy
/// </summary>
public class SubmoduleReference : IEquatable<SubmoduleReference>, IModuleReference, IValidityManagedEntity
{
    private EventHandler? updated;

    private readonly ValidityManager manager;

    /// <summary>
    /// Create submodule reference given top-level module and path to submodule
    /// </summary>
    /// <param name="topLevelModule"></param>
    /// <param name="path"></param>
    public SubmoduleReference(IModule topLevelModule, IEnumerable<IInstantiation> path)
    {
        TopLevelModule = topLevelModule;
        Path = new([.. path]);
        manager = new ValidityManager<object>(this, [topLevelModule, .. path]);
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

    /// <inheritdoc/>
    public IModule TopLevelModule { get; }

    /// <inheritdoc/>
    public ReadOnlyCollection<IInstantiation> Path { get; }

    /// <summary>
    /// Final module in path
    /// </summary>
    public IModule FinalModule => Path.Select(i => i.InstantiatedModule).LastOrDefault(TopLevelModule);

    /// <summary>
    /// Gets submodule or signal reference inside this submodule
    /// If there are names of both, the submodule is returned
    /// </summary>
    /// <param name="name">Name of signal or submodule</param>
    /// <returns></returns>
    public IModuleReference this[string name]
    {
        get
        {
            // Try to find instantiation
            IInstantiation? instantiation = FinalModule.Instantiations.FirstOrDefault(i => i.Name == name);
            if (instantiation is not null)
                return new SubmoduleReference(TopLevelModule, [.. Path, instantiation]);

            // Try to find signal
            ISingleNodeNamedSignal? signal = FinalModule.AllModuleSignals.OfType<INamedSignal>().SelectMany(s => s.ToSingleNodeSignals).FirstOrDefault(s => s.Name == name);
            if (signal is not null)
                return new SignalReference(this, signal);

            throw new SubmodulePathException($"Could not find {name} in module {FinalModule}");
        }
    }

    /// <summary>
    /// Get child submodule reference given instantiation
    /// </summary>
    /// <param name="instantiation"></param>
    /// <returns></returns>
    public SubmoduleReference GetChildSubmoduleReference(IInstantiation instantiation) => new(TopLevelModule, [.. Path, instantiation]);

    /// <summary>
    /// Try to get child submodule reference given instantiation name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="reference">Output reference</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool TryGetChildSubmoduleReference(string name, out SubmoduleReference? reference)
    {
        // Try to find instantiation
        IInstantiation? instantiation = FinalModule.Instantiations.FirstOrDefault(i => i.Name == name);
        if (instantiation is not null)
        {
            reference = new SubmoduleReference(TopLevelModule, [.. Path, instantiation]);
            return true;
        }

        reference = null;
        return false;
    }

    /// <summary>
    /// Get child submodule reference given instantiation name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="SubmodulePathException">Thrown if not found</exception>
    public SubmoduleReference GetChildSubmoduleReference(string name) =>
        TryGetChildSubmoduleReference(name, out SubmoduleReference? reference) && reference is not null ? reference : throw new SubmodulePathException($"Could not find instantiation {name} in module {FinalModule}");

    /// <summary>
    /// Get child signal reference given signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public SignalReference GetChildSignalReference(INamedSignal signal) => new(this, signal);

    /// <summary>
    /// Try to get child signal reference given signal name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="reference">Output reference</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool TryGetChildSignalReference(string name, out SignalReference? reference)
    {
        // Try to find signal
        INamedSignal? signal = FinalModule.AllModuleSignals.OfType<INamedSignal>().FirstOrDefault(s => s.Name == name);
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
    /// <exception cref="SubmodulePathException">Thrown if not found</exception>
    public SignalReference GetChildSignalReference(string name) =>
        TryGetChildSignalReference(name, out SignalReference? reference) && reference is not null ? reference : throw new SubmodulePathException($"Could not find signal {name} in module {FinalModule}");

    /// <summary>
    /// Compare to another submodule reference
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SubmoduleReference? other)
    {
        return other is not null && TopLevelModule.Equals(other.TopLevelModule) && Path.SequenceEqual(other.Path);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SubmoduleReference submodule && Equals(submodule);

    /// <summary>
    /// Returns true if two references are equal
    /// </summary>
    /// <param name="reference1"></param>
    /// <param name="reference2"></param>
    /// <returns></returns>
    public static bool operator==(SubmoduleReference reference1, SubmoduleReference reference2) => reference1.Equals(reference2);
    
    /// <summary>
    /// Returns true if two references are not equal
    /// </summary>
    /// <param name="reference1"></param>
    /// <param name="reference2"></param>
    /// <returns></returns>
    public static bool operator!=(SubmoduleReference reference1, SubmoduleReference reference2) => !reference1.Equals(reference2);
    
    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(TopLevelModule);
        foreach (IInstantiation instantiation in Path)
            hash.Add(instantiation);
        return hash.ToHashCode();
    }

    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;
        IModule module = TopLevelModule;
        foreach (IInstantiation instantiation in Path)
        {
            if (!instantiation.ParentModule.Equals(module))
                exception = new SubmodulePathException($"Parent module of instantiation ({instantiation}) doesn't match {module}");
            if (!module.Instantiations.Contains(instantiation))
                exception = new SubmodulePathException($"Module {module} does not contain given instantiation ({instantiation})");
            module = instantiation.InstantiatedModule;
        }
        return exception is null;
    }
}