using SpiceSharp.Components;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// Interface for an instantiation of one module inside of another (parent).
/// It is assumed that the parent module and instantiated module won't change
/// </summary>
public interface IInstantiation : IValidityManagedEntity
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
    /// Convert to VHDL. 
    /// For instantiation, not component declaration. 
    /// </summary>
    /// <returns></returns>
    public string GetVhdlStatement();

    /// <summary>
    /// Given a dictionary mapping modules to subcircuit definition objects,
    /// generate a Spice circuit object for this instantiation
    /// </summary>
    /// <param name="subcircuitDefinitions">Dictionary mapping modules to Spice# subcircuit definition objects so that they only one is used per module</param>
    /// <returns></returns>
    public SpiceCircuit GetSpice(Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions);

    /// <inheritdoc/>
    public string ToString();

    /// <summary>
    /// Signals of instantiation matching given direction
    /// </summary>
    /// <param name="portDirection"></param>
    /// <returns></returns>
    public IEnumerable<INamedSignal> GetSignals(PortDirection portDirection) => PortMapping.Where(kvp => kvp.Key.Direction == portDirection).Select(kvp => kvp.Value);
}