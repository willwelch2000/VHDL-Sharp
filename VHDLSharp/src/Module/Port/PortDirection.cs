namespace VHDLSharp.Modules;

/// <summary>
/// Directions that a port can have with respect to a module
/// </summary>
public enum PortDirection
{
    /// <summary>
    /// Input to the module
    /// </summary>
    Input,
    /// <summary>
    /// Output from the module
    /// </summary>
    Output,
    /// <summary>
    /// Input/output
    /// </summary>
    Bidirectional
}