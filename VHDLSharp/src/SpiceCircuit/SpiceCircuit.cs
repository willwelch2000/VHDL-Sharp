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

        ISet<ISubcircuitDefinition> defsUsedHere = GetSubcircuitDefinitions(false);

        // Generate inner context to be used in subcircuits, initializing models context to all the models here + those in the given context
        // and subcircuit definitions context to those in the given context + subcircuit definitions used here
        CircuitContext innerContext = new()
        {
            Models = new HashSet<IEntity>([.. circuitContext.Models, .. CircuitElements.Where(e => e.IsModel())]),
            SubcircuitDefinitions = new HashSet<ISubcircuitDefinition>([.. circuitContext.SubcircuitDefinitions, .. defsUsedHere]),
        };

        // Declare subcircuits used here
        foreach (ISubcircuitDefinition subcircuitDef in defsUsedHere.Except(circuitContext.SubcircuitDefinitions))
        {
            if (subcircuitDef is not INamedSubcircuitDefinition namedSubcircuitDef)
                throw new Exception("Subcircuit definition must implement ISubcircuitDefinition to be turned into a string");
            SpiceSubcircuit subcircuit = new(namedSubcircuitDef.Name, subcircuitDef.Pins, subcircuitDef.Entities);
            sb.AppendLine(subcircuit.AsSubcircuitString(innerContext) + "\n");
        }

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
                sb.AppendLine(element.GetSpice());

        return sb.ToString().TrimEnd(); // Skip last new line
    }

    /// <summary>
    /// Convert to Spice subcircuit object given name and pins
    /// </summary>
    /// <param name="name">Name to use for subcircuit</param>
    /// <param name="pins">Array of pins for subcircuit</param>
    /// <returns></returns>
    public SpiceSubcircuit ToSpiceSubcircuit(string name, string[] pins) => new(name, pins, CircuitElements);

    /// <summary>
    /// Convert to Spice subcircuit object given name and pins
    /// </summary>
    /// <param name="module">Module that is the basis for subcircuit</param>
    /// <param name="pins">Array of pins for subcircuit</param>
    /// <returns></returns>
    public SpiceSubcircuit ToSpiceSubcircuit(IModule module, string[] pins) => new(module, pins, CircuitElements);

    /// <summary>
    /// Get subcircuits used by this circuit
    /// </summary>
    /// <param name="recursive">If true, looks inside used subcircuits</param>
    /// <returns></returns>
    public ISet<ISubcircuitDefinition> GetSubcircuitDefinitions(bool recursive)
    {
        HashSet<ISubcircuitDefinition> alreadyFound = [];
        Queue<ISubcircuitDefinition> definitions = [];

        // Subcircuits directly used
        foreach (Subcircuit instance in CircuitElements.OfType<Subcircuit>())
            if (alreadyFound.Add(instance.Parameters.Definition))
                definitions.Enqueue(instance.Parameters.Definition);

        if (!recursive)
            return alreadyFound;

        // Recurse
        while (definitions.TryDequeue(out ISubcircuitDefinition? subcircuitDef))
        {
            foreach (Subcircuit instance in subcircuitDef.Entities.OfType<Subcircuit>())
                if (alreadyFound.Add(instance.Parameters.Definition))
                    definitions.Enqueue(instance.Parameters.Definition);
        }

        return alreadyFound;
    }

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
    /// Generate a <see cref="SpiceCircuit"/> by combining several other circuits with this one.
    /// Ignores duplicate entities so that common entities/models don't appear twice
    /// </summary>
    /// <param name="otherCircuits">Circuits to combine with this</param>
    /// <returns></returns>
    public SpiceCircuit CombineWith(IEnumerable<SpiceCircuit> otherCircuits) => Combine([this, .. otherCircuits]);

    /// <summary>
    /// Generate a <see cref="SpiceCircuit"/> by combining several circuits.
    /// Ignores duplicate entities so that common entities/models don't appear twice
    /// </summary>
    /// <param name="circuits">Circuits to combine</param>
    /// <returns></returns>
    public static SpiceCircuit Combine(IEnumerable<SpiceCircuit> circuits)
    {
        return new(circuits.SelectMany(c => c.CircuitElements).Distinct()); // Ignore duplicate entities
    }
}