using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;

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
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals { get; } = [];

    /// <inheritdoc/>
    public override Dimension Dimension { get; }

    /// <inheritdoc/>
    protected override string GetVhdlStatementWithoutCheck(INamedSignal outputSignal)
    {
        return$"{outputSignal.GetVhdlName()} <= \"{Value.ToBinaryString(outputSignal.Dimension.NonNullValue)}\";";
    }

    /// <inheritdoc/>
    protected override SpiceCircuit GetSpiceWithoutCheck(INamedSignal outputSignal, string uniqueId)
    {
        int i = 0;
        // Loop through single-node signals and apply corresponding bit of Value
        List<IEntity> entities = [];
        foreach (ISingleNodeNamedSignal singleNodeSignal in outputSignal.ToSingleNodeSignals)
            entities.Add(new VoltageSource(SpiceUtil.GetSpiceName(uniqueId, i, "value"), singleNodeSignal.GetSpiceName(), "0", (Value & 1<<i++) > 0 ? SpiceUtil.VDD : 0));
        return new SpiceCircuit(entities); // No common entities needed
    }

    /// <inheritdoc/>
    protected override int GetOutputValueWithoutCheck(RuleBasedSimulationState state, SignalReference outputSignal) => Value;
}