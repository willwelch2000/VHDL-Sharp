using SpiceSharp.Components;

namespace VHDLSharp.SpiceCircuits;

/// <summary>
/// Extension of <see cref="ISubcircuitDefinition"/> that includes a name property
/// </summary>
public interface INamedSubcircuitDefinition : ISubcircuitDefinition
{
    /// <summary>
    /// Get name of definition
    /// </summary>
    public string Name { get; }
}