using VHDLSharp.Behaviors;
using VHDLSharp.Conditions;
using VHDLSharp.LogicTree;

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

    /// <summary>
    /// Bitwise And of this signal and another
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static And<ISignal> And(this ISignal signal, ILogicallyCombinable<ISignal> other) => new(signal, other);

    /// <summary>
    /// Bitwise Or of this signal and another
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Or<ISignal> Or(this ISignal signal, ILogicallyCombinable<ISignal> other) => new(signal, other);

    /// <summary>
    /// Logical Not of this signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public static Not<ISignal> Not(this ISignal signal) => new(signal);

    /// <summary>
    /// Bitwise And of this signal and others
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static And<ISignal> And(this ISignal signal, IEnumerable<ILogicallyCombinable<ISignal>> others) => new([signal, .. others]);

    /// <summary>
    /// Bitwise Or of this signal and others
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static Or<ISignal> Or(this ISignal signal, IEnumerable<ILogicallyCombinable<ISignal>> others) => new([signal, .. others]);


    // Methods that are shortcuts for adding behaviors

    /// <summary>
    /// Assign a specified behavior to the signal
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="behavior"></param>
    /// <returns>Assigned behavior</returns>
    public static T AssignBehavior<T>(this INamedSignal signal, T behavior) where T : IBehavior
    {
        signal.ParentModule.SignalBehaviors[signal] = behavior;
        return behavior;
    }

    /// <summary>
    /// Assign a specified value to the signal as a <see cref="ValueBehavior"/>
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="value"></param>
    /// <returns>Assigned behavior</returns>
    public static ValueBehavior AssignBehavior(this INamedSignal signal, int value) => signal.AssignBehavior(new ValueBehavior(value));

    /// <summary>
    /// Assign a specified expression to the signal as a <see cref="LogicBehavior"/>
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="expression"></param>
    /// <returns>Assigned behavior</returns>
    public static LogicBehavior AssignBehavior(this INamedSignal signal, ILogicallyCombinable<ISignal> expression) => signal.AssignBehavior(new LogicBehavior(expression));

    /// <summary>
    /// Remove behavior assignment from this signal
    /// </summary>
    /// <param name="signal"></param>
    public static void RemoveBehavior(this INamedSignal signal) => signal.ParentModule.SignalBehaviors.Remove(signal);

    /// <summary>
    /// Convert named signal to <see cref="LogicBehavior"/>
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public static LogicBehavior ToBehavior(this INamedSignal signal) => new(signal);
}