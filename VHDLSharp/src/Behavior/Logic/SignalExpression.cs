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
}