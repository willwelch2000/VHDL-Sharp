
namespace VHDLSharp;

/// <summary>
/// Logical and between two logical expressions
/// </summary>
/// <param name="inputs"></param>
public class And(params LogicExpression[] inputs) : LogicExpression
{
    private readonly LogicExpression[] inputs = inputs;

    /// <summary>
    /// Accessor for inputs
    /// </summary>
    public IEnumerable<LogicExpression> Inputs => inputs.AsEnumerable();

    /// <inheritdoc/>
    public override IEnumerable<SingleNodeSignal> Signals => inputs.SelectMany(i => i.Signals);

    /// <inheritdoc/>
    public override string ToVhdl => string.Join(" and ", Inputs.Select(i => $"({i.ToVhdl})"));
}