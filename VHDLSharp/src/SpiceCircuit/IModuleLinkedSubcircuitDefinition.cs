using VHDLSharp.Modules;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Interface for subcircuit definition linked to a <see cref="IModule"/>
/// </summary>
public interface IModuleLinkedSubcircuitDefinition : INamedSubcircuitDefinition
{
    /// <summary>
    /// Module this subcircuit definition is linked to
    /// </summary>
    public IModule Module { get; }

    /// <inheritdoc/>
    string INamedSubcircuitDefinition.Name => Module.Name;
}