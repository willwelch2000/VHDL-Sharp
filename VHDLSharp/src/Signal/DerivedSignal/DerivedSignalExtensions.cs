namespace VHDLSharp.Signals;

/// <summary>
/// Class that has extension methods for generating derived signals
/// </summary>
public static class DerivedSignalExtensions
{
    /// <summary>
    /// Add another signal to this signal
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static AddedSignal Plus(this IModuleSpecificSignal signal, IModuleSpecificSignal other) => new(signal, other);
}