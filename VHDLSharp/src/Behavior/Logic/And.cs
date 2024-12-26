
namespace VHDLSharp;

/// <summary>
/// Logical and between two logical expressions
/// </summary>
public class And : LogicExpression
{
    private readonly LogicExpression[] inputs;

    /// <summary>
    /// Create new And expression
    /// </summary>
    /// <param name="inputs"></param>
    /// <exception cref="Exception"></exception>
    public And(params LogicExpression[] inputs)
    {
        this.inputs = inputs;
        Dimension = inputs.FirstOrDefault(i => i?.Dimension is not null, null)?.Dimension;
        CheckValid();
    }

    /// <summary>
    /// Accessor for inputs
    /// </summary>
    public IEnumerable<LogicExpression> Inputs => inputs.AsEnumerable();

    /// <inheritdoc/>
    public override int? Dimension { get; }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> Signals => inputs.SelectMany(i => i.Signals);

    /// <inheritdoc/>
    public override string ToVhdl => string.Join(" and ", Inputs.Select(i => $"({i.ToVhdl})"));
}