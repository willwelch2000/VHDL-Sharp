namespace VHDLSharp.Exceptions;

/// <summary>
/// Exception when signals are defined in a circular pattern illegally
/// </summary>
public class CircularSignalException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public CircularSignalException() : base("A circular signal exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public CircularSignalException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public CircularSignalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}