using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Modules;
using VHDLSharp.Utility;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Class used to define a Spice subcircuit, using Spice# entities
/// </summary>
public class SpiceSubcircuit : SpiceCircuit
{
    private readonly IEnumerable<IEntity> circuitElements;

    private string? name = null;

    /// <summary>
    /// Constructor given module, pins, and circuit elements
    /// </summary>
    /// <param name="pins">names of pins to be included in subcircuit definition</param>
    /// <param name="circuitElements">entities in the circuit</param>
    /// <param name="module">module this is linked to, if applicable</param>
    public SpiceSubcircuit(IModule module, IEnumerable<string> pins, IEnumerable<IEntity> circuitElements) : base(circuitElements)
    {
        Module = module;
        Pins = [.. pins];
        this.circuitElements = circuitElements;
    }

    /// <summary>
    /// Constructor given name, pins, and circuit elements
    /// </summary>
    /// <param name="name">name of the subcircuit</param>
    /// <param name="pins">names of pins to be included in subcircuit definition</param>
    /// <param name="circuitElements">entities in the circuit</param>
    public SpiceSubcircuit(string name, IEnumerable<string> pins, IEnumerable<IEntity> circuitElements) : base(circuitElements)
    {
        this.name = name;
        Pins = [.. pins];
        this.circuitElements = circuitElements;
    }

    /// <summary>
    /// Name of the subcircuit
    /// </summary>
    public string Name => Module?.Name ?? name ?? throw new("Should be impossible");

    /// <summary>
    /// Module to which this is linked, if applicable
    /// </summary>
    public IModule? Module { get; }

    /// <summary>
    /// Names of pins to be included in subcircuit definition
    /// </summary>
    public string[] Pins { get; }

    /// <summary>
    /// Get object as a Spice# <see cref="SubcircuitDefinition"/>
    /// </summary>
    /// <returns></returns>
    public INamedSubcircuitDefinition AsSubcircuit() => Module is null ? 
        new NamedSubcircuitDefinition(Name, CircuitElements, Pins) : 
        new ModuleLinkedSubcircuitDefinition(Module, CircuitElements, Pins);

    /// <summary>
    /// Attempt to get this subcircuit as a <see cref="IModuleLinkedSubcircuitDefinition"/>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IModuleLinkedSubcircuitDefinition AsModuleLinkedSubcircuit() => AsSubcircuit() as ModuleLinkedSubcircuitDefinition ?? 
        throw new Exception("No module is linked to this subcircuit");


    /// <summary>
    /// Get object as a string, including used subcircuits
    /// </summary>
    /// <returns></returns>
    public string AsSubcircuitString() => AsSubcircuitString(new());
    
    /// <summary>
    /// Internal version of <see cref="AsSubcircuitString()"/> function that accepts context for models 
    /// and subcircuit definitions that are declared at a higher level so that they can be ignored
    /// </summary>
    /// <param name="circuitContext">Context for models and subcircuit definitions declared at a higher level</param>
    /// <returns></returns>
    internal string AsSubcircuitString(CircuitContext circuitContext) => $".subckt {Name} {string.Join(' ', Pins)}\n" + AsString(circuitContext).AddIndentation(1) + $"\n.ends {Name}";
}