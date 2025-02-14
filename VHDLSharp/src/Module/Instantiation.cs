using System.Text;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Utility;

namespace VHDLSharp.Modules;

/// <summary>
/// Instantiation of one module inside of another (parent)
/// </summary>
public class Instantiation
{
    private EventHandler? instantiatedModuleUpdated;

    /// <summary>
    /// Create new instantiation given instantiated module and parent module
    /// </summary>
    /// <param name="instantiatedModule">Module that is instantiated</param>
    /// <param name="parentModule">The module inside which this instantiation exists</param>
    /// <param name="name">Name of instantiation</param>
    public Instantiation(Module instantiatedModule, Module parentModule, string name)
    {
        InstantiatedModule = instantiatedModule;
        PortMapping = new(instantiatedModule, parentModule);
        ParentModule = parentModule;
        Name = name;
        instantiatedModule.ModuleUpdated += (object? sender, EventArgs e) => instantiatedModuleUpdated?.Invoke(this, e);
    }

    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public Module InstantiatedModule { get; }

    /// <summary>
    /// Mapping of module's ports to parent's signals (connections to module)
    /// </summary>
    public PortMapping PortMapping { get; }

    /// <summary>
    /// Module that contains module instantiation
    /// </summary>
    public Module ParentModule { get; }

    /// <summary>
    /// Name of instantiation
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Name used for spice instantiation
    /// </summary>
    public string SpiceName => $"X{Name}";

    /// <summary>
    /// Event called whenever referenced module is updated
    /// </summary>
    public event EventHandler? InstantiatedModuleUpdated
    {
        add
        {
            instantiatedModuleUpdated -= value; // remove if already present
            instantiatedModuleUpdated += value;
        }
        remove => instantiatedModuleUpdated -= value;
    }

    /// <summary>
    /// Convert to spice. 
    /// Looks at each port in the instantiated module and appends the corresponding signal to the spice
    /// </summary>
    /// <returns></returns>
    public string ToSpice() => $"{SpiceName} " + string.Join(' ', InstantiatedModule.Ports.SelectMany(p => PortMapping[p].ToSingleNodeSignals).Select(s => s.GetSpiceName()));

    /// <summary>
    /// Convert to VHDL. 
    /// For instantiation, not component declaration. 
    /// </summary>
    /// <returns></returns>
    public string ToVhdl()
    {
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

    /// <summary>
    /// Get list of instantiations as list of entities for Spice#
    /// </summary>
    /// <param name="instantiations">Instantiations to add</param>
    public static IEnumerable<IEntity> GetSpiceSharpEntities(IEnumerable<Instantiation> instantiations)
    {
        // Make subcircuit definitions for all distinct modules
        Dictionary<Module, SubcircuitDefinition> subcircuitDefinitions = [];
        foreach (Module submodule in instantiations.Select(i => i.InstantiatedModule).Distinct())
            subcircuitDefinitions[submodule] = submodule.ToSpiceSharpSubcircuit();

        // Add instantiations
        int i = 0;
        foreach (Instantiation instantiation in instantiations)
        {
            string[] nodes = [.. instantiation.InstantiatedModule.Ports.SelectMany(p => instantiation.PortMapping[p].ToSingleNodeSignals).Select(s => s.GetSpiceName())];
            yield return new Subcircuit($"X{i}", subcircuitDefinitions[instantiation.InstantiatedModule], nodes);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{InstantiatedModule} in {ParentModule}";
    }
}