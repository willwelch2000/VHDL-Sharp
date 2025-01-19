using SpiceSharp.Components;
using SpiceSharp.Entities;

namespace VHDLSharp.Modules;

/// <summary>
/// Instantiation of one module inside of another (parent)
/// </summary>
/// <param name="module">module that is instantiated</param>
/// <param name="parent">the module inside which this instantiation exists</param>
public class Instantiation(Module module, Module parent)
{
    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public Module Module { get; private init; } = module;

    /// <summary>
    /// Mapping of module's ports to parent's signals (connections to module)
    /// </summary>
    public PortMapping PortMapping { get; private init; } = new(module, parent);

    /// <summary>
    /// Module inside which the module is instantiated
    /// </summary>
    public Module Parent { get; private init; } = parent;

    /// <summary>
    /// Convert to spice
    /// Looks at each port in the instantiated module and appends the corresponding signal to the spice
    /// </summary>
    /// <param name="index">Unique int provided to this instantiation so that it can have a unique name</param>
    /// <returns></returns>
    public string ToSpice(int index) => $"X{index} " + string.Join(' ', Module.Ports.SelectMany(p => PortMapping[p].ToSingleNodeNamedSignals).Select(s => s.ToSpice()));

    /// <summary>
    /// Get list of instantiations as list of entities for Spice#
    /// </summary>
    /// <param name="instantiations">Instantiations to add</param>
    public static IEnumerable<IEntity> GetSpiceSharpEntities(IEnumerable<Instantiation> instantiations)
    {
        // Make subcircuit definitions for all distinct modules
        Dictionary<Module, SubcircuitDefinition> subcircuitDefinitions = [];
        foreach (Module submodule in instantiations.Select(i => i.Module).Distinct())
            subcircuitDefinitions[submodule] = submodule.ToSpiceSubcircuit();

        // Add instantiations
        int i = 0;
        foreach (Instantiation instantiation in instantiations)
        {
            string[] nodes = [.. instantiation.Module.Ports.SelectMany(p => instantiation.PortMapping[p].ToSingleNodeNamedSignals).Select(s => s.ToSpice())];
            yield return new Subcircuit($"X{i}", subcircuitDefinitions[instantiation.Module], nodes);
        }
    }
}