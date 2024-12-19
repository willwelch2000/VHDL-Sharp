namespace VHDLSharp;

/// <summary>
/// Logical expression that just refers to a <see cref="SingleNodeSignal"/> 
/// </summary>
/// <param name="signal"></param>
public class SignalExpression(SingleNodeSignal signal) : LogicExpression
{
    /// <summary>
    /// Accessor for signal
    /// </summary>
    public SingleNodeSignal Signal { get; private init; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<SingleNodeSignal> Signals => [Signal];

    /// <inheritdoc/>
    public override string ToVhdl => Signal.ToVhdl;

    /// <summary>
    /// Convert to signal
    /// </summary>
    /// <param name="exp"></param>
    public static implicit operator SingleNodeSignal(SignalExpression exp) => exp.Signal;

    /// <summary>
    /// Convert signal to signal expression
    /// </summary>
    /// <param name="signal"></param>
    public static implicit operator SignalExpression(SingleNodeSignal signal) => new(signal);
}