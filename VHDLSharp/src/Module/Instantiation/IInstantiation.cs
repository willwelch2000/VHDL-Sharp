using SpiceSharp.Components;
using SpiceSharp.Entities;

namespace VHDLSharp.Modules;

/// <summary>
/// Interface for an instantiation of one module inside of another (parent)
/// </summary>
public interface IInstantiation
{
    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public IModule InstantiatedModule => PortMapping.InstantiatedModule;

    /// <summary>
    /// Mapping of module's ports to parent's signals (connections to module)
    /// </summary>
    public PortMapping PortMapping { get; }

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
    public string GetSpice();

    /// <summary>
    /// Convert to VHDL. 
    /// For instantiation, not component declaration. 
    /// </summary>
    /// <returns></returns>
    public string GetVhdlStatement();

    /// <summary>
    /// Given a dictionary mapping modules to subcircuit definition objects,
    /// generate a Spice# subcircuit object for this instantiation
    /// </summary>
    /// <param name="subcircuitDefinitions">Dictionary mapping modules to Spice# subcircuit definition objects</param>
    /// <returns></returns>
    public Subcircuit GetSpiceSharpSubcircuit(Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions);

    /// <inheritdoc/>
    public string ToString();

    /// <summary>
    /// Get list of instantiations as list of entities for Spice#
    /// </summary>
    /// <param name="instantiations">Instantiations to add</param>
    public static IEnumerable<IEntity> GetSpiceSharpEntities(IEnumerable<IInstantiation> instantiations)
    {
        // Make subcircuit definitions for all distinct modules
        Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions = [];
        foreach (IModule submodule in instantiations.Select(i => i.InstantiatedModule).Distinct())
            subcircuitDefinitions[submodule] = submodule.GetSpiceSharpSubcircuit();

        // Add instantiations
        foreach (IInstantiation instantiation in instantiations)
            yield return instantiation.GetSpiceSharpSubcircuit(subcircuitDefinitions);
    }
}