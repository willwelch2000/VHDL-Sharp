
namespace VHDLSharp;

/// <summary>
/// Logical or between two logical expressions
/// </summary>
/// <param name="inputs"></param>
public class Or(params LogicExpression[] inputs) : LogicExpression
{
    private readonly LogicExpression[] inputs = inputs;

    /// <summary>
    /// Accessor for inputs
    /// </summary>
    public IEnumerable<LogicExpression> Inputs => inputs.AsEnumerable();

    /// <inheritdoc/>
    public override IEnumerable<SingleNodeSignal> Signals => inputs.SelectMany(i => i.Signals);
}