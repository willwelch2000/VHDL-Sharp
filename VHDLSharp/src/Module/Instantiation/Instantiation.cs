using System.Text;
using SpiceSharp.Components;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// Instantiation of one module inside of another (parent)
/// TODO options:
/// 1. Combine PortMapping into this, make this a dictionary
/// 2. Move all error-checking into PortMapping, don't add any module as child here
///     This seems weird bc it feels like instantiated module should be child
/// 3. Move all error-checking here away from PortMapping, add instantiated module as child
/// 4. Add instantiated module as child here, but keep error-checking in PortMapping
/// </summary>
public class Instantiation : IInstantiation, IValidityManagedEntity
{
    private readonly ValidityManager validityManager;

    /// <summary>
    /// Create new instantiation given instantiated module and parent module
    /// </summary>
    /// <param name="instantiatedModule">Module that is instantiated</param>
    /// <param name="parentModule">The module inside which this instantiation exists</param>
    /// <param name="name">Name of instantiation</param>
    public Instantiation(IModule instantiatedModule, IModule parentModule, string name)
    {
        InstantiatedModule = instantiatedModule;
        PortMapping = new(instantiatedModule, parentModule);
        ParentModule = parentModule;
        Name = name;
        validityManager = new(this);
        validityManager.AddTrackedObjectIfEntity(instantiatedModule);
        validityManager.AddTrackedObjectIfEntity(PortMapping);
    }

    ValidityManager IValidityManagedEntity.ValidityManager => validityManager;

    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public IModule InstantiatedModule { get; }

    /// <summary>
    /// Mapping of module's ports to parent's signals (connections to module)
    /// </summary>
    public PortMapping PortMapping { get; }

    /// <summary>
    /// Module that contains module instantiation
    /// </summary>
    public IModule ParentModule { get; }

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
    public string GetSpice() => $"{SpiceName} " + string.Join(' ', InstantiatedModule.Ports.SelectMany(p => PortMapping[p].ToSingleNodeSignals).Select(s => s.GetSpiceName()));

    /// <summary>
    /// Convert to VHDL. 
    /// For instantiation, not component declaration. 
    /// </summary>
    /// <returns></returns>
    public string GetVhdlStatement()
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

    /// <inheritdoc/>
    public Subcircuit GetSpiceSharpSubcircuit(Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions, int uniqueId)
    {
        string[] nodes = [.. InstantiatedModule.Ports.SelectMany(p => PortMapping[p].ToSingleNodeSignals).Select(s => s.GetSpiceName())];
        return new Subcircuit($"X{uniqueId}", subcircuitDefinitions[InstantiatedModule], nodes);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{InstantiatedModule} in {ParentModule}";
}