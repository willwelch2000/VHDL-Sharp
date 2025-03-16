using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Utility;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Class used to define a Spice subcircuit, using Spice# entities
/// </summary>
/// <param name="name">name of the subcircuit</param>
/// <param name="pins">names of pins to be included in subcircuit definition</param>
/// <param name="circuitElements">entities in the circuit</param>
public class SpiceSubcircuit(string name, IEnumerable<string> pins, IEnumerable<IEntity> circuitElements) : SpiceCircuit(circuitElements)
{
    /// <summary>
    /// Name of the subcircuit
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Names of pins to be included in subcircuit definition
    /// </summary>
    public string[] Pins { get; } = [.. pins];

    /// <summary>
    /// Get object as a Spice# <see cref="SubcircuitDefinition"/>
    /// </summary>
    /// <returns></returns>
    public SubcircuitDefinition AsSubcircuit() => new(CircuitElements, Pins);

    /// <summary>
    /// Get object as a string, including used subcircuits
    /// </summary>
    /// <returns></returns>
    public string AsSubcircuitString() => $".subckt {Name} {string.Join(' ', Pins)}\n" + AsString().AddIndentation(1) + $"\n.ends {Name}";
}