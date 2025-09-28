using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SpiceSharp.Components;
using VHDLSharp.Exceptions;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// Instantiation of one module inside of another (parent)
/// </summary>
public class Instantiation : IInstantiation, IValidityManagedEntity
{
    private readonly ValidityManager validityManager;

    private readonly ObservableCollection<object> childEntities;

    /// <summary>
    /// Create new instantiation given instantiated module and parent module
    /// </summary>
    /// <param name="instantiatedModule">Module that is instantiated</param>
    /// <param name="parentModule">The module inside which this instantiation exists</param>
    /// <param name="name">Name of instantiation</param>
    public Instantiation(IModule instantiatedModule, IModule parentModule, string name)
    {
        PortMapping = new(instantiatedModule, parentModule);
        Name = name;

        // Initialize validity manager and list of child entities
        // PortMapping is included, so no additional error-checking is needed, because PortMapping being valid implies this is valid
        childEntities = [PortMapping];
        validityManager = new ValidityManager<object>(this, childEntities);
    }

    ValidityManager IValidityManagedEntity.ValidityManager => validityManager;

    /// <summary>
    /// Mapping of module's ports to parent's signals (connections to module)
    /// </summary>
    public PortMapping PortMapping { get; }

    // Both modules are accessed from port mapping so that they're only in one place

    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public IModule InstantiatedModule => PortMapping.InstantiatedModule;

    /// <summary>
    /// Module that contains module instantiation
    /// </summary>
    public IModule ParentModule => PortMapping.ParentModule;

    /// <summary>
    /// Name of instantiation
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Convert to VHDL. 
    /// For instantiation, not component declaration. 
    /// </summary>
    /// <returns></returns>
    public string GetVhdlStatement()
    {
        if (!validityManager.IsValid(out Exception? issue))
            throw new InvalidException("Instantiation is invalid", issue);

        if (!PortMapping.IsComplete(out string? reason))
            throw new IncompleteException($"Instantiation not yet complete: {reason}");

        StringBuilder sb = new();
        sb.AppendLine($"{Name} : {InstantiatedModule.Name}");
        sb.AppendLine("port map (".AddIndentation(1));
        sb.AppendJoin(",\n", PortMapping.Select(
            kvp => $"{kvp.Key.Signal.GetVhdlDeclaration()} => {kvp.Value.GetVhdlDeclaration()}".AddIndentation(2)
        ));
        sb.AppendLine();
        sb.AppendLine(");".AddIndentation(1));

        return sb.ToString();
    }

    /// <inheritdoc/>
    public SpiceCircuit GetSpice(ISet<IModuleLinkedSubcircuitDefinition> existingModuleLinkedSubcircuits)
    {
        if (!validityManager.IsValid(out Exception? issue))
            throw new InvalidException("Instantiation is invalid", issue);

        if (!PortMapping.IsComplete(out string? reason))
            throw new IncompleteException($"Instantiation not yet complete: {reason}");

        if (existingModuleLinkedSubcircuits.FirstOrDefault(def => def.Module.Equals(InstantiatedModule)) is not IModuleLinkedSubcircuitDefinition subcircuitDef)
            subcircuitDef = InstantiatedModule.GetSpice(existingModuleLinkedSubcircuits).AsModuleLinkedSubcircuit();

        string[] nodes = [.. InstantiatedModule.Ports.SelectMany(p => PortMapping[p].ToSingleNodeSignals).Select(s => s.GetSpiceName())];
        Subcircuit entity = new(Name, subcircuitDef, nodes);

        return new([entity]);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{InstantiatedModule} in {ParentModule}";

    /// <inheritdoc/>
    public bool IsComplete([MaybeNullWhen(true)] out string reason) => PortMapping.IsComplete(out reason);
}