namespace VHDLSharp.Exceptions;

/// <summary>
/// Exception when descending in hierarchy through subcircuits
/// Includes trying to find invalid subcircuit or signal
/// </summary>
public class SubcircuitPathException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public SubcircuitPathException() : base("A subcircuit path exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public SubcircuitPathException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public SubcircuitPathException(string message, Exception innerException) : base(message, innerException)
    {
    }
}