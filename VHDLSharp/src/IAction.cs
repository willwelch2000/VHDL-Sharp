namespace VHDLSharp;

/// <summary>
/// An action that can happen at a given <see cref="IDigitalEvent"/> 
/// </summary>
public interface IDigitalAction
{
    /// <summary>
    /// Get all of the signals used in this action
    /// </summary>
    public IEnumerable<Signal> InvolvedSignals { get; }
}