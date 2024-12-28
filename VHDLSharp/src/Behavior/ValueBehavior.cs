using VHDLSharp.Utility;

namespace VHDLSharp;

/// <summary>
/// Behavior where a direct value is assigned to the signal
/// </summary>
public class ValueBehavior : CombinationalBehavior
{
    /// <summary>
    /// Generate new value behavior
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ValueBehavior(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 0");
        Value = value;
        if (value == 0)
            Dimension = new(1, null);
        else
        {
            double max = Math.Floor(Math.Log2(value)) + 1;
            Dimension = new((int)max, null);
        }
    }

    /// <summary>
    /// Value to be assigned to signal
    /// </summary>
    public int Value { get; }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals { get; } = [];

    /// <inheritdoc/>
    public override Dimension Dimension { get; }

    /// <inheritdoc/>
    public override string ToVhdl(ISignal outputSignal) => $"{outputSignal} <= {Value.ToBinaryString(outputSignal.Dimension.NonNullValue)};";
}