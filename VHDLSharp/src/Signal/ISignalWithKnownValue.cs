namespace VHDLSharp.Signals;

/// <summary>
/// A signal that has a known value
/// </summary>
public interface ISignalWithKnownValue : ISignal
{
    /// <summary>
    /// Value of the signal
    /// </summary>
    public int Value { get; }
}