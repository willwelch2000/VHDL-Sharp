using SpiceSharp.Components;
using SpiceSharp.Entities;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Class passed as an argument when generating Spice as a string
/// </summary>
internal class CircuitContext
{
    /// <summary>
    /// Models declared at a higher level that can be ignored here
    /// </summary>
    internal ISet<IEntity> Models { get; set; } = new HashSet<IEntity>();

    /// <summary>
    /// Subcircuit definitions declared at a higher level that can be ignored here, 
    /// mapped to their names
    /// </summary>
    internal IDictionary<ISubcircuitDefinition, string> SubcircuitDefinitions { get; set; } = new Dictionary<ISubcircuitDefinition, string>();
}