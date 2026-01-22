using System.Collections.ObjectModel;
using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Interface for a reference in a module hierarchy (signal or submodule)
/// </summary>
public interface IModuleReference
{
    /// <summary>
    /// Module used as top-level in hierarchy
    /// </summary>
    public IModule TopLevelModule { get; }

    /// <summary>
    /// Path of instantiations that leads to submodule.
    /// Each instantiation must be in the previous one's module
    /// </summary>
    public ReadOnlyCollection<IInstantiation> Path { get; }
}