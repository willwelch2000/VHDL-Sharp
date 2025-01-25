using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Stimulus that applies a digital pulse
/// </summary>
public class PulseStimulus : Stimulus
{
    /// <summary>
    /// Delay time for pulse
    /// </summary>
    public double DelayTime { get; set; }

    /// <summary>
    /// Pulse width for pulse
    /// </summary>
    public double PulseWidth { get; set; }

    /// <summary>
    /// Period for pulse
    /// </summary>
    public double Period { get; set; }

    /// <inheritdoc/>
    protected override string ToSpiceGivenSingleNodeSignal(SingleNodeNamedSignal signal, string uniqueId) =>
        $"V{Util.GetSpiceName(uniqueId, 0, "pulse")} {signal.ToSpice()} 0 PULSE(0 {Util.VDD} {DelayTime} {Util.RiseFall} {Util.RiseFall} {PulseWidth} {Period})";

    /// <inheritdoc/>
    protected override IEnumerable<IEntity> ToSpiceSharpEntitiesGivenSingleNodeSignal(SingleNodeNamedSignal signal, string uniqueId)
    {
        Pulse pulse = new(0, Util.VDD, DelayTime, Util.RiseFall, Util.RiseFall, PulseWidth, Period);
        yield return new VoltageSource($"V{Util.GetSpiceName(uniqueId, 0, "pulse")}", signal.ToSpice(), "0", pulse);
    }
}