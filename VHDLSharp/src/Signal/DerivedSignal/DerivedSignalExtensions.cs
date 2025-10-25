namespace VHDLSharp.Signals.Derived;

/// <summary>
/// Class that has extension methods for generating derived signals
/// </summary>
public static class DerivedSignalExtensions
{
    /// <summary>
    /// Add another signal to this signal
    /// </summary>
    /// <param name="signal">Starting signal</param>
    /// <param name="other">Signal to add</param>
    /// <param name="includeCarryOut">If true, the resulting signal has an additional bit that comes from the carry-out of the addition</param>
    /// <returns></returns>
    public static AddedSignal Plus(this IModuleSpecificSignal signal, IModuleSpecificSignal other, bool includeCarryOut = false) => new(signal, other, includeCarryOut);

    /// <summary>
    /// Subtract another signal from this signal. The two signals are treated as signed. 
    /// </summary>
    /// <param name="signal">Starting signal</param>
    /// <param name="other">Signal to subtract</param>
    /// <param name="extraBit">If true, the resulting signal has an additional bit to avoid overflow</param>
    /// <returns>Signed signal that is the subtraction of the two inputs</returns>
    public static IDerivedSignal Minus(this IModuleSpecificSignal signal, IModuleSpecificSignal other, bool extraBit = false) => throw new NotImplementedException();

    /// <summary>
    /// Extend this signal to become a larger-dimension signal
    /// </summary>
    /// <param name="signal">Starting signal</param>
    /// <param name="totalBits">The total number of bits for the output signal</param>
    /// <param name="signed">If true, the added bits will match the MSB</param>
    /// <returns></returns>
    public static IDerivedSignal Extend(this IModuleSpecificSignal signal, int totalBits, bool signed = false) => throw new NotImplementedException();

    /// <summary>
    /// Concatenate this signal with another
    /// </summary>
    /// <param name="signal">Starting signal</param>
    /// <param name="other">Other signal to concatenate after the starting signal</param>
    /// <returns></returns>
    public static IDerivedSignal ConcatWith(this IModuleSpecificSignal signal, IModuleSpecificSignal other) => throw new NotImplementedException();
}