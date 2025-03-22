using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Modules;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Subcircuit definition linked to a <see cref="IModule"/>
/// </summary>
/// <param name="name"></param>
/// <param name="entities"></param>
/// <param name="pins"></param>
public class NamedSubcircuitDefinition(string name, IEntityCollection entities, params string[] pins) : INamedSubcircuitDefinition
{
    private readonly SubcircuitDefinition backendDefinition = new(entities, pins);

    /// <inheritdoc/>
    public IEntityCollection Entities => backendDefinition.Entities;

    /// <inheritdoc/>
    public IReadOnlyList<string> Pins => backendDefinition.Pins;
    
    /// <inheritdoc/>
    public string Name { get; } = name;

    /// <inheritdoc/>
    public ISubcircuitDefinition Clone() => new NamedSubcircuitDefinition(Name, Entities, [.. Pins]);
}