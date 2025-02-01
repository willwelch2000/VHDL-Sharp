using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
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
        // Following produces correct minimum dimension given the value
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
    public override string ToVhdl(NamedSignal outputSignal)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return$"{outputSignal} <= \"{Value.ToBinaryString(outputSignal.Dimension.NonNullValue)}\";";
    }

    /// <inheritdoc/>
    public override string ToSpice(NamedSignal outputSignal, string uniqueId)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        string toReturn = "";
        int i = 0;
        // Loop through single-node signals and apply corresponding bit of Value
        foreach (SingleNodeNamedSignal singleNodeSignal in outputSignal.ToSingleNodeSignals)
            // TODO voltage sources could be standardized better
            toReturn += $"V{Util.GetSpiceName(uniqueId, i, "value")} {singleNodeSignal.ToSpice()} 0 {((Value & 1<<i++) > 0 ? Util.VDD : 0)}\n";
        return toReturn;
    }

    /// <inheritdoc/>
    public override IEnumerable<IEntity> GetSpiceSharpEntities(NamedSignal outputSignal, string uniqueId)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        int i = 0;
        // Loop through single-node signals and apply corresponding bit of Value
        foreach (SingleNodeNamedSignal singleNodeSignal in outputSignal.ToSingleNodeSignals)
            yield return new VoltageSource(Util.GetSpiceName(uniqueId, i, "value"), singleNodeSignal.ToSpice(), "0", (Value & 1<<i++) > 0 ? Util.VDD : 0);
    }
}