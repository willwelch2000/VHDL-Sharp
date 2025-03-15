using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Signals;
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
    protected override string GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId) =>
        $"V{SpiceUtil.GetSpiceName(uniqueId, 0, "const")} {signal.GetSpiceName()} 0 {(Value ? SpiceUtil.VDD.ToString() : "0")}";

    /// <inheritdoc/>
    protected override IEnumerable<IEntity> GetSpiceSharpEntitiesGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId)
    {
        // DC voltage source at signal with VDD or 0
        yield return new VoltageSource(SpiceUtil.GetSpiceName(uniqueId, 0, "const"), signal.GetSpiceName(), "0", Value ? SpiceUtil.VDD : 0);
    }
}