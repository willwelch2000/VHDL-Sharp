using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Class used to define a Spice circuit, using Spice# entities
/// </summary>
/// <param name="circuitElements">entities in the circuit</param>
public class SpiceCircuit(IEnumerable<IEntity> circuitElements)
{
    /// <summary>
    /// Entities in the circuit
    /// </summary>
    public IEntityCollection CircuitElements { get; } = new Circuit(circuitElements);

    /// <summary>
    /// Subcircuit names, if known. 
    /// If the name isn't given for a subcircuit, it will check the utility class and otherwise just generate a numeric name
    /// </summary>
    public Dictionary<SubcircuitDefinition, string> SubcircuitNames = [];

    /// <summary>
    /// Get object as a Spice# <see cref="Circuit"/>
    /// </summary>
    /// <returns></returns>
    public Circuit AsCircuit() => [.. CircuitElements];

    /// <summary>
    /// Get object as a string, including used subcircuits
    /// </summary>
    /// <returns></returns>
    public string AsString()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Convert to Spice subcircuit object given name and pins
    /// </summary>
    /// <param name="name">Name to use for subcircuit</param>
    /// <param name="pins">Array of pins for subcircuit</param>
    /// <returns></returns>
    public SpiceSubcircuit ToSpiceSubcircuit(string name, string[] pins) => new(name, pins, circuitElements);

    /// <summary>
    /// Generate a <see cref="SpiceCircuit"/> by combining several objects.
    /// Ignores duplicate entities so that common entities/models don't appear twice
    /// </summary>
    /// <param name="circuits"></param>
    /// <returns></returns>
    public static SpiceCircuit Combine(IEnumerable<SpiceCircuit> circuits)
    {
        // Use set to ignore duplicates
        HashSet<IEntity> entities = [.. circuits.SelectMany(c => c.CircuitElements)];
        return new(entities);
    }
}