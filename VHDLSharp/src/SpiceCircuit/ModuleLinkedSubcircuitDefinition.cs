using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Modules;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Subcircuit definition linked to a <see cref="IModule"/>
/// </summary>
/// <param name="module"></param>
/// <param name="entities"></param>
/// <param name="pins"></param>
public class ModuleLinkedSubcircuitDefinition(IModule module, IEntityCollection entities, params string[] pins) : IModuleLinkedSubcircuitDefinition
{
    private readonly SubcircuitDefinition backendDefinition = new(entities, pins);

    /// <inheritdoc/>
    public IEntityCollection Entities => backendDefinition.Entities;

    /// <inheritdoc/>
    public IReadOnlyList<string> Pins => backendDefinition.Pins;

    /// <summary>
    /// Module this subcircuit definition is linked to
    /// </summary>
    public IModule Module { get; } = module;

    /// <inheritdoc/>
    public ISubcircuitDefinition Clone() => new ModuleLinkedSubcircuitDefinition(Module, Entities, [.. Pins]);
}