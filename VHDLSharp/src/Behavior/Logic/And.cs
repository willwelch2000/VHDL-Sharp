
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
        if (inputs.Select(i => i.Dimension).Where(i => i is not null).Distinct().Count() > 1)
            throw new Exception("Inputs must all have the same dimension");
        this.inputs = inputs;
        Dimension = inputs.FirstOrDefault(i => i?.Dimension is not null, null)?.Dimension;
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