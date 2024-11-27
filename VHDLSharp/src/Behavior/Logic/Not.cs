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
    public override IEnumerable<SingleNodeSignal> Signals => Input.Signals;
}