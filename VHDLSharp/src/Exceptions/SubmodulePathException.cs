namespace VHDLSharp.Exceptions;

/// <summary>
/// Exception when descending in hierarchy through submodules. 
/// Includes trying to find invalid submodule or signal
/// </summary>
public class SubmodulePathException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public SubmodulePathException() : base("A submodule path exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public SubmodulePathException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public SubmodulePathException(string message, Exception innerException) : base(message, innerException)
    {
    }
}