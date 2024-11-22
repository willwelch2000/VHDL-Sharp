namespace VHDLSharp;

/// <summary>
/// An event upon which <see cref="IDigitalAction"/>s can happen
/// </summary>
public interface IDigitalEvent
{
    /// <summary>
    /// Get all of the signals referenced in this event
    /// </summary>
    public IEnumerable<Signal> InvolvedSignals { get; }
}