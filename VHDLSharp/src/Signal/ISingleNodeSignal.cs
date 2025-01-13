namespace VHDLSharp.Signals;

/// <summary>
/// Interface for signal of any type that has exactly one node
/// Dimension must be 1
/// </summary>
public interface ISingleNodeSignal : ISignal
{
    /// <summary>
    /// Get representation in SPICE
    /// </summary>
    /// <returns></returns>
    public string ToSpice();
}