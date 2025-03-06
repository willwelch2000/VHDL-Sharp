namespace VHDLSharp.Validation;

/// <summary>
/// Exception when a component is not valid, but the user 
/// attempts to do something with it
/// </summary>
public class InvalidException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public InvalidException() : base("An invalid exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public InvalidException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public InvalidException(string message, Exception innerException) : base(message, innerException)
    {
    }
}