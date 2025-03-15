using System.Text;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Documentation;
using SpiceSharp.Entities;
using VHDLSharp.Utility;

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
    public Dictionary<ISubcircuitDefinition, string> SubcircuitNames { get; set; } = [];

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
        StringBuilder sb = new();

        // Add subcircuit declarations
        HashSet<string> subcircuitNames = [];
        int i = 0;
        IEnumerable<Subcircuit> instantiations = CircuitElements.Where(e => e is Subcircuit).Select(e => (Subcircuit)e);
        foreach (ISubcircuitDefinition subcircuitDef in instantiations.Select(i => i.Parameters.Definition).Distinct())
        {
            string? name = SubcircuitNames.TryGetValue(subcircuitDef, out string? val) ? val : (SpiceUtil.SubcircuitNames.TryGetValue(subcircuitDef, out val) ? val : null);
            if (name is null)
                do
                    name = $"subcircuit{i}";
                while (!subcircuitNames.Add(name));
            else
                subcircuitNames.Add(name);

            SpiceSubcircuit subcircuit = new(name, subcircuitDef.Pins, subcircuitDef.Entities);
            sb.AppendLine(subcircuit.AsSubcircuitString().AddIndentation(1));
        }

        // Sort entities into groups for ordering
        int groups = 3;
        List<IEntity>[] orderedElements = new List<IEntity>[groups];
        for (int j = 0; j < groups; j++)
            orderedElements[j] = [];

        IEntity[] commonEntities = [.. SpiceUtil.CommonEntities];
        foreach (IEntity element in CircuitElements)
        {
            int group = element is Mosfet1Model or Mosfet2Model or Mosfet3Model ? 0 :
                        commonEntities.Contains(element) ? 1 : 2;
            orderedElements[group].Add(element);
        }

        // Add all entities by group
        for (int j = 0; j < groups; j++)
            foreach (IEntity element in orderedElements[j])
                sb.AppendLine(element.GetSpice());

        return sb.ToString();
    }

    /// <summary>
    /// Convert to Spice subcircuit object given name and pins
    /// </summary>
    /// <param name="name">Name to use for subcircuit</param>
    /// <param name="pins">Array of pins for subcircuit</param>
    /// <returns></returns>
    public SpiceSubcircuit ToSpiceSubcircuit(string name, string[] pins) => new(name, pins, circuitElements);

    /// <summary>
    /// Add on common entities from Spice Util
    /// </summary>
    /// <returns></returns>
    internal SpiceCircuit WithCommonEntities()
    {
        foreach (IEntity entity in SpiceUtil.CommonEntities)
            if (!CircuitElements.Contains(entity))
                CircuitElements.Add(entity);
        return this;
    }

    /// <summary>
    /// Generate a <see cref="SpiceCircuit"/> by combining several objects.
    /// Ignores duplicate entities so that common entities/models don't appear twice
    /// </summary>
    /// <param name="circuits"></param>
    /// <returns></returns>
    public static SpiceCircuit Combine(IEnumerable<SpiceCircuit> circuits) => new(circuits.SelectMany(c => c.CircuitElements).Distinct()); // Ignore duplicate entities
}