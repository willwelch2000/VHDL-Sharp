using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for a signal that uses more complicated logic (in the form of an <see cref="IInstantiation"/>)
/// to assign a value to a signal. A <see cref="LinkedSignal"/> should be assigned the value determined
/// by the relevant logic. 
/// </summary>
public interface IDerivedSignal : ISignal
{
    // /// <summary>
    // /// Name of derived signal. 
    // /// If no name is provided, one will be temporarily assigned when needed by the module
    // /// </summary>
    // public string? Name { get; set; }

    /// <summary>
    /// The named signal whose value will be determined by this derived signal.
    /// If unassigned, a temporary signal will be created and assigned by the <see cref="IModule"/>. 
    /// The module and dimension of the linked signal must match this. 
    /// </summary>
    public INamedSignal? LinkedSignal { get; set; }

    /// <summary>
    /// Generate an instantation object that assigns this derived signal to the <see cref="LinkedSignal"/>. 
    /// The instantiation should have the given <paramref name="instanceName"/>. 
    /// <paramref name="moduleName"/> can be used if creating a new module to avoid repeats. 
    /// </summary>
    /// <param name="moduleName">Name for a new module, if needed</param>
    /// <param name="instanceName">Name for the returned instantiation</param>
    /// <returns></returns>
    public IInstantiation Compile(string moduleName, string instanceName);
}

public abstract class DerivedSignal : IDerivedSignal
{
    // /// <inheritdoc/>
    // public string? Name { get; set; } = null;

    /// <inheritdoc/>
    public INamedSignal? LinkedSignal { get; set; }
}