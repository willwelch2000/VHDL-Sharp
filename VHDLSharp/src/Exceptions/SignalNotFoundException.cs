namespace VHDLSharp.Exceptions;

/// <summary>
/// Exception when a signal is not found when it is expected
/// </summary>
public class SignalNotFoundException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public SignalNotFoundException() : base("A signal not found exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public SignalNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public SignalNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}