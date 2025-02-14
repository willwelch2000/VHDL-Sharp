using System.Collections.ObjectModel;
using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Interface for a reference in a circuit hierarchy (signal or subcircuit)
/// </summary>
public interface ICircuitReference
{
    /// <summary>
    /// Module used as top-level in circuit
    /// </summary>
    public Module TopLevelModule { get; }

    /// <summary>
    /// Path of instantiations that leads to subcircuit
    /// Each instantiation must be in the previous one's module
    /// </summary>
    public ReadOnlyCollection<IInstantiation> Path { get; }
}