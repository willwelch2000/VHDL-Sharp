namespace VHDLSharp;

/// <summary>
/// Interface defining a behavior that makes up a module
/// </summary>
public interface IDigitalBehavior
{
    /// <summary>
    /// Get all of the signals used in this behavior
    /// </summary>
    public IEnumerable<Signal> InvolvedSignals { get; }
}