namespace VHDLSharp;

/// <summary>
/// Interface defining a behavior that makes up a module
/// </summary>
public interface IDigitalBehavior
{
    /// <summary>
    /// Get all of the input signals used in this behavior
    /// </summary>
    public IEnumerable<ISignal> InputSignals { get; }

    /// <summary>
    /// Get the output signal for this behavior
    /// </summary>
    public ISignal OutputSignal { get; }
}