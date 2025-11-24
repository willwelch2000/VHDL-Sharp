namespace VHDLSharp.Exceptions;

/// <summary>
/// Exception when recursion happens illegally
/// </summary>
public class IllegalRecursionException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public IllegalRecursionException() : base("An illegal recursion exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public IllegalRecursionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public IllegalRecursionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}