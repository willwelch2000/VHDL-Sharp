namespace VHDLSharp.Exceptions;

/// <summary>
/// Exception when a component is not compatible with a given signal
/// </summary>
public class IncompatibleSignalException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public IncompatibleSignalException() : base("An incomplete exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public IncompatibleSignalException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public IncompatibleSignalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}