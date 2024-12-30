using VHDLSharp.LogicTree;
using VHDLSharp.Utility;

namespace VHDLSharp;

/// <summary>
/// Literal value that can be used in expressions
/// </summary>
public class Literal : ISignal
{
    /// <summary>
    /// Generate new literal given value
    /// </summary>
    /// <param name="value">Must be >= 0</param>
    /// <param name="dimension">Must be >= 0</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Literal(int value, int dimension)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 0");

        Value = value;


        if (value == 0)
        {
            if (dimension < 1)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Must be >= 0");
        }
        else
        {
            double min = Math.Floor(Math.Log2(value)) + 1;
            if (dimension < min)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Must be large enough for value");
        }

        Dimension = new(dimension);
    }

    /// <summary>
    /// Literal value
    /// </summary>
    public int Value { get; }

    /// <inheritdoc/>
    public DefiniteDimension Dimension { get; }
    
    /// <inheritdoc/>
    public IEnumerable<ISignal> BaseObjects => [];

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        return Dimension.Compatible(VHDLSharp.Dimension.CombineWithoutCheck(other.BaseObjects.Select(o => o.Dimension)));
    }

    /// <inheritdoc/>
    public string ToLogicString() => $"\"{Value.ToBinaryString(Dimension.NonNullValue)}\"";

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public string ToVhdlInExpression(DefiniteDimension dimension) => ToLogicString();
}