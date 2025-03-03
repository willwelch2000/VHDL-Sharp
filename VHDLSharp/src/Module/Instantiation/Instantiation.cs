using System.Collections.ObjectModel;
using System.Text;
using SpiceSharp.Components;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// Instantiation of one module inside of another (parent)
/// </summary>
public class Instantiation : IInstantiation, IValidityManagedEntity
{
    private readonly ValidityManager validityManager;

    private readonly ObservableCollection<object> trackedEntities;

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

        // Initialize validity manager and list of tracked entities
        // PortMapping is included, so no additional error-checking is needed, because PortMapping being valid implies this is valid
        trackedEntities = [PortMapping];
        validityManager = new(this, trackedEntities);
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
    /// Name used for spice instantiation
    /// </summary>
    public string SpiceName => $"X{Name}";

    /// <summary>
    /// Convert to spice. 
    /// Looks at each port in the instantiated module and appends the corresponding signal to the spice
    /// </summary>
    /// <returns></returns>
    public string GetSpice()
    {
        if (!PortMapping.IsComplete())
            throw new Exception("Port mapping is not complete");

        return $"{SpiceName} " + string.Join(' ', InstantiatedModule.Ports.SelectMany(p => PortMapping[p].ToSingleNodeSignals).Select(s => s.GetSpiceName()));
    }

    /// <summary>
    /// Convert to VHDL. 
    /// For instantiation, not component declaration. 
    /// </summary>
    /// <returns></returns>
    public string GetVhdlStatement()
    {
        if (!PortMapping.IsComplete())
            throw new Exception("Port mapping is not complete");

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
    public Subcircuit GetSpiceSharpSubcircuit(Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions)
    {
        if (!PortMapping.IsComplete())
            throw new Exception("Port mapping is not complete");

        string[] nodes = [.. InstantiatedModule.Ports.SelectMany(p => PortMapping[p].ToSingleNodeSignals).Select(s => s.GetSpiceName())];
        return new Subcircuit($"X{SpiceName}", subcircuitDefinitions[InstantiatedModule], nodes);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{InstantiatedModule} in {ParentModule}";
}