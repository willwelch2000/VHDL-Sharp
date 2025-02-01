namespace VHDLSharp.Exceptions;

/// <summary>
/// Exception when a component is not complete, but the user 
/// attempts to do something with it
/// </summary>
public class IncompleteException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public IncompleteException() : base("An incomplete exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public IncompleteException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public IncompleteException(string message, Exception innerException) : base(message, innerException)
    {
    }
}