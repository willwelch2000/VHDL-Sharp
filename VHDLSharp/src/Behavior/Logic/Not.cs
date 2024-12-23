namespace VHDLSharp;

/// <summary>
/// Logical not of a logical expressions
/// </summary>
/// <param name="input"></param>
public class Not(LogicExpression input) : LogicExpression
{
    /// <summary>
    /// Accessor for input
    /// </summary>
    public LogicExpression Input { get; private init; } = input;

    /// <inheritdoc/>
    public override int? Dimension { get; } = input.Dimension;

    /// <inheritdoc/>
    public override IEnumerable<ISignal> Signals => Input.Signals;

    /// <inheritdoc/>
    public override string ToVhdl => $"not ({Input.ToVhdl})";
}