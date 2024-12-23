namespace VHDLSharp;

/// <summary>
/// Logical expression that just refers to a <see cref="SingleNodeSignal"/> 
/// </summary>
/// <param name="signal"></param>
public class SignalExpression(ISignal signal) : LogicExpression
{
    /// <summary>
    /// Accessor for signal
    /// </summary>
    public ISignal Signal { get; private init; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<ISignal> Signals => [Signal];

    /// <inheritdoc/>
    public override int? Dimension => Signal.Dimension;

    /// <inheritdoc/>
    public override string ToVhdl => Signal.Name;
}