
namespace VHDLSharp;

/// <summary>
/// Logic expression that is a constant boolean value
/// TODO might get rid of this
/// </summary>
/// <param name="value"></param>
public class BooleanExpression(bool value) : LogicExpression
{
    /// <summary>
    /// Boolean value of expression
    /// </summary>
    public bool Value => value;

    /// <inheritdoc/>
    public override IEnumerable<ISignal> Signals => [];

    /// <inheritdoc/>
    public override int? Dimension => null;

    /// <inheritdoc/>
    public override string ToVhdl => Value.ToString();

    /// <summary>
    /// Convert bool to <see cref="BooleanExpression"/>
    /// </summary>
    /// <param name="exp"></param>
    public static implicit operator bool(BooleanExpression exp) => exp.Value;

    /// <summary>
    /// Convert to bool
    /// </summary>
    /// <param name="b"></param>
    public static implicit operator BooleanExpression(bool b) => new(b);
}