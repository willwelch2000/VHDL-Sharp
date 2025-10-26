namespace VHDLSharp.Signals;

/// <summary>
/// Exception when an <see cref="IDerivedSignal"/> is not yet linked to an 
/// <see cref="INamedSignal"/> but the linked signal is needed
/// </summary>
public class UnlinkedDerivedSignalException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public UnlinkedDerivedSignalException() : base("Derived signal must have a linked signal assigned to it.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public UnlinkedDerivedSignalException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public UnlinkedDerivedSignalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}