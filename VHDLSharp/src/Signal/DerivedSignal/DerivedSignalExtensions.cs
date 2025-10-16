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
    /// <param name="includeCarryOut">If true, the resulting signal has an additional bit that comes from the carry-out of the addition</param>
    /// <returns></returns>
    public static AddedSignal Plus(this IModuleSpecificSignal signal, IModuleSpecificSignal other, bool includeCarryOut=false) => new(signal, other, includeCarryOut);
}