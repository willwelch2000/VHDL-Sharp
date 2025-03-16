using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Behavior where a direct value is assigned to the signal
/// </summary>
public class ValueBehavior : Behavior, ICombinationalBehavior
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
    public override IEnumerable<INamedSignal> NamedInputSignals { get; } = [];

    /// <inheritdoc/>
    public override Dimension Dimension { get; }

    /// <inheritdoc/>
    public override string GetVhdlStatement(INamedSignal outputSignal)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Value behavior must be valid to convert to VHDL");
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return$"{outputSignal} <= \"{Value.ToBinaryString(outputSignal.Dimension.NonNullValue)}\";";
    }

    /// <inheritdoc/>
    public override SpiceCircuit GetSpice(INamedSignal outputSignal, string uniqueId)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Value behavior must be valid to convert to Spice# entities");
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");

        int i = 0;
        // Loop through single-node signals and apply corresponding bit of Value
        List<IEntity> entities = [];
        foreach (ISingleNodeNamedSignal singleNodeSignal in outputSignal.ToSingleNodeSignals)
            entities.Add(new VoltageSource(SpiceUtil.GetSpiceName(uniqueId, i, "value"), singleNodeSignal.GetSpiceName(), "0", (Value & 1<<i++) > 0 ? SpiceUtil.VDD : 0));
        return new SpiceCircuit(entities); // No common entities needed
    }
}