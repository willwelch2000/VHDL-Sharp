using SpiceSharp.Components;
using SpiceSharp.Entities;

namespace VHDLSharp.Modules;

/// <summary>
/// Instantiation of one module inside of another (parent)
/// </summary>
public class Instantiation : IHasParentModule
{
    private EventHandler? submoduleUpdated;

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
        instantiatedModule.ModuleUpdated += (object? sender, EventArgs e) => submoduleUpdated?.Invoke(this, e);
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
    public event EventHandler? SubmoduleUpdated
    {
        add
        {
            submoduleUpdated -= value; // remove if already present
            submoduleUpdated += value;
        }
        remove => submoduleUpdated -= value;
    }

    /// <summary>
    /// Convert to spice
    /// Looks at each port in the instantiated module and appends the corresponding signal to the spice
    /// </summary>
    /// <returns></returns>
    public string ToSpice() => $"{SpiceName} " + string.Join(' ', InstantiatedModule.Ports.SelectMany(p => PortMapping[p].ToSingleNodeSignals).Select(s => s.ToSpice()));

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
            string[] nodes = [.. instantiation.InstantiatedModule.Ports.SelectMany(p => instantiation.PortMapping[p].ToSingleNodeSignals).Select(s => s.ToSpice())];
            yield return new Subcircuit($"X{i}", subcircuitDefinitions[instantiation.InstantiatedModule], nodes);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{InstantiatedModule} in {ParentModule}";
    }
}