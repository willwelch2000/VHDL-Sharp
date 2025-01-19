using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Behaviors;

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
            double min = Math.Floor(Math.Log2(value)) + 1;
            Dimension = new((int)min, null);
        }
    }

    /// <summary>
    /// Value to be assigned to signal
    /// </summary>
    public int Value { get; }

    /// <inheritdoc/>
    public override IEnumerable<NamedSignal> NamedInputSignals { get; } = [];

    /// <inheritdoc/>
    public override Dimension Dimension { get; }

    /// <inheritdoc/>
    public override string ToVhdl(NamedSignal outputSignal) => $"{outputSignal} <= \"{Value.ToBinaryString(outputSignal.Dimension.NonNullValue)}\";";

    /// <inheritdoc/>
    public override string ToSpice(NamedSignal outputSignal, string uniqueId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override IEnumerable<IEntity> GetSpiceSharpEntities(NamedSignal outputSignal, string uniqueId)
    {
        throw new NotImplementedException();
    }
}