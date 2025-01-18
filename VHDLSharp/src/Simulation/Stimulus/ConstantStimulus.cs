using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Stimulus that applies a constant digital value
/// </summary>
public class ConstantStimulus : Stimulus
{
    /// <summary>
    /// Digital value of stimulus
    /// </summary>
    public bool Value { get; set; }

    /// <inheritdoc/>
    protected override string ToSpiceGivenSingleNodeSignal(SingleNodeNamedSignal signal, string uniqueId) =>
        $"V{Util.GetSpiceName(uniqueId, 0, "const")} {signal.ToSpice()} 0 {(Value ? Util.VDD.ToString() : "0")}";
}