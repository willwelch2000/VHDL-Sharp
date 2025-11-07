using VHDLSharp.Conditions;

namespace VHDLSharp.Signals;

/// <summary>
/// Class that has extension methods for signals
/// </summary>
public static class SignalExtensions
{
    /// <summary>
    /// Condition that is true if <paramref name="mainSignal"/> is greater than <paramref name="comparisonSignal"/>
    /// </summary>
    /// <param name="mainSignal">Main signal for comparison</param>
    /// <param name="comparisonSignal">Comparison signal</param>
    /// <param name="signed">If the signals should be treated as signed</param>
    /// <returns></returns>
    public static Comparison GreaterThan(this ISignal mainSignal, ISignal comparisonSignal, bool signed = false) => new(mainSignal, comparisonSignal, false, signed);
    
    /// <summary>
    /// Condition that is true if <paramref name="mainSignal"/> is less than <paramref name="comparisonSignal"/>
    /// </summary>
    /// <param name="mainSignal">Main signal for comparison</param>
    /// <param name="comparisonSignal">Comparison signal</param>
    /// <param name="signed">If the signals should be treated as signed</param>
    /// <returns></returns>
    public static Comparison LessThan(this ISignal mainSignal, ISignal comparisonSignal, bool signed = false) => new(mainSignal, comparisonSignal, true, signed);

    // TODO make main signal ISignal
    /// <summary>
    /// Condition that is true if <paramref name="mainSignal"/> is equal to <paramref name="comparisonSignal"/>
    /// </summary>
    /// <param name="mainSignal">Main signal for comparison</param>
    /// <param name="comparisonSignal">Comparison signal</param>
    /// <returns></returns>
    public static Equality EqualTo(this IModuleSpecificSignal mainSignal, ISignal comparisonSignal) => new(mainSignal, comparisonSignal);

    // TODO make these work for ISingleNodeSignal
    /// <summary>
    /// Rising-edge condition for <paramref name="signal"/>
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public static RisingEdge RisingEdge(this ISingleNodeNamedSignal signal) => new(signal);

    /// <summary>
    /// Falling-edge condition for <paramref name="signal"/>
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public static FallingEdge FallingEdge(this ISingleNodeNamedSignal signal) => new(signal);

    /// <summary>
    /// High condition for <paramref name="signal"/>
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public static High IsHigh(this ISingleNodeNamedSignal signal) => new(signal);
    
    /// <summary>
    /// Low condition for <paramref name="signal"/>
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public static Low IsLow(this ISingleNodeNamedSignal signal) => new(signal);
}