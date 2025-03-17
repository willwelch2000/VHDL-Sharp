using System.Text;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Modules;
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
    /// Mapping of subcircuit definitions to their associated modules, if known. 
    /// If it exists, it will use the module to get the subcircuit definition's Spice name. 
    /// If it doesn't exist, it will check the subcircuits in the utility class for names and otherwise just generate a numeric name. 
    /// This is also useful for subcircuit definition re-use, so that the subcircuit definitions can be reused
    /// </summary>
    public Dictionary<ISubcircuitDefinition, IModule> SubcircuitModules { get; set; } = [];

    /// <summary>
    /// Get object as a Spice# <see cref="Circuit"/>
    /// </summary>
    /// <returns></returns>
    public Circuit AsCircuit() => [.. CircuitElements];

    /// <summary>
    /// Get object as a string, including used subcircuits
    /// </summary>
    /// <returns></returns>
    public string AsString() => AsString(new());

    /// <summary>
    /// Internal version of <see cref="AsString()"/> function that accepts context for models 
    /// and subcircuit definitions that are declared at a higher level so that they can be ignored
    /// </summary>
    /// <param name="circuitContext">Context for models and subcircuit definitions declared at a higher level</param>
    /// <returns></returns>
    internal string AsString(CircuitContext circuitContext)
    {
        StringBuilder sb = new();

        // Generate inner context to be used in subcircuits, initializing models context to all the models here + those in the given context
        // and subcircuit definitions context to those in the given context
        CircuitContext innerContext = new()
        {
            Models = new HashSet<IEntity>([.. circuitContext.Models, .. CircuitElements.Where(e => e.IsModel())]),
            SubcircuitDefinitions = new Dictionary<ISubcircuitDefinition, string>(circuitContext.SubcircuitDefinitions),
        };

        // Decide on names for all subcircuit definitions except those declared at a higher level, get list of all subcircuit defs to declare
        IEnumerable<Subcircuit> instantiations = CircuitElements.Where(e => e is Subcircuit).Select(e => (Subcircuit)e);
        List<SpiceSubcircuit> subcircuitsToDeclare = [];
        foreach (ISubcircuitDefinition subcircuitDef in instantiations.Select(i => i.Parameters.Definition).Except(circuitContext.SubcircuitDefinitions.Keys).Distinct())
        {
            // Find name, passing inner context that contains all just-added definitions for modification
            string name = GetNameAndAdd(subcircuitDef, innerContext);
            subcircuitsToDeclare.Add(new(name, subcircuitDef.Pins, subcircuitDef.Entities));
        }

        // Declare all subcircuit definitions needed
        foreach (SpiceSubcircuit subcircuitToDeclare in subcircuitsToDeclare)
            sb.AppendLine(subcircuitToDeclare.AsSubcircuitString(innerContext) + "\n");

        // Create groups for ordering entities
        int groups = 3;
        List<IEntity>[] orderedElements = new List<IEntity>[groups];
        for (int j = 0; j < groups; j++)
            orderedElements[j] = [];

        // Sort all entities except those declared at a higher level into groups
        IEntity[] commonEntities = [.. SpiceUtil.CommonEntities];
        foreach (IEntity element in CircuitElements.Except(circuitContext.Models))
        {
            // Models, then common entities, then everything else
            int group = element.IsModel() ? 0 : commonEntities.Contains(element) ? 1 : 2;
            orderedElements[group].Add(element);
        }

        // Add all entities by group--pass along mapping of all subcircuit names, from the inner context
        for (int j = 0; j < groups; j++)
            foreach (IEntity element in orderedElements[j])
                sb.AppendLine(element.GetSpice(innerContext.SubcircuitDefinitions));

        return sb.ToString().TrimEnd(); // Skip last new line
    }

    // Modifies circuit context's subcircuit definitions
    private string GetNameAndAdd(ISubcircuitDefinition subcircuitDef, CircuitContext circuitContext)
    {
        // Don't have to check pre-existing context--it shouldn't have made it here if it's been declared already

        // Check dictionary for subcircuit names
        if (SubcircuitModules.TryGetValue(subcircuitDef, out IModule? module))
        {
            circuitContext.SubcircuitDefinitions.Add(subcircuitDef, module.Name);
            return module.Name;
        }

        // Then, check SpiceUtil subcircuit definition dictionary
        if (SpiceUtil.SubcircuitNames.TryGetValue(subcircuitDef, out string? name))
        {
            circuitContext.SubcircuitDefinitions.Add(subcircuitDef, name);
            return name;
        }

        // Otherwise, generate a name that is in none of the dictionaries and add it to the context
        int i = 0;
        do
            name = $"subcircuit{i++}";
        while (!SubcircuitModules.Select(kvp => kvp.Value.Name).Contains(name) && !SpiceUtil.SubcircuitNames.Values.Contains(name) && !circuitContext.SubcircuitDefinitions.TryAdd(subcircuitDef, name));
        return name;
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
    public static SpiceCircuit Combine(IEnumerable<SpiceCircuit> circuits)
    {
        SpiceCircuit combinedCircuit = new(circuits.SelectMany(c => c.CircuitElements).Distinct()); // Ignore duplicate entities
        // Combine subcircuit names
        foreach (var kvp in circuits.SelectMany(c => c.SubcircuitModules))
            if (!combinedCircuit.SubcircuitModules.TryAdd(kvp.Key, kvp.Value))
                if (kvp.Value != combinedCircuit.SubcircuitModules[kvp.Key])
                    throw new Exception("Conflicting modules for subcircuit definition");
        return combinedCircuit;
    }
}