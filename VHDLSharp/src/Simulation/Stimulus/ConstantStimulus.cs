using SpiceSharp.Components;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Stimulus that applies a constant digital value
/// </summary>
public class ConstantStimulus : Stimulus
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public ConstantStimulus() {}

    /// <summary>
    /// Constructor given value
    /// </summary>
    /// <param name="value"></param>
    public ConstantStimulus(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Digital value of stimulus
    /// </summary>
    public bool Value { get; set; } = false;
    
    /// <inheritdoc/>
    protected override SpiceCircuit GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId)
    {
        // DC voltage source at signal with VDD or 0
        return new([new VoltageSource(SpiceUtil.GetSpiceName(uniqueId, 0, "const"), signal.GetSpiceName(), "0", Value ? SpiceUtil.VDD : 0)]);
    }

    /// <inheritdoc/>
    protected override bool GetValue(double currentTime) => Value;

    /// <inheritdoc/>
    protected override IEnumerable<double> GetIndependentEventTimes(double simulationLength) => [];
}