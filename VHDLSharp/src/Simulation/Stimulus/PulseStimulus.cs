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
    /// Default constructor
    /// </summary>
    public PulseStimulus()
    {
        DelayTime = 0.5e-3;
        PulseWidth = 0.5e-3;
        Period = 1e-3;
    }

    /// <summary>
    /// Constructor given parameters
    /// </summary>
    /// <param name="delayTime"></param>
    /// <param name="pulseWidth"></param>
    /// <param name="period"></param>
    public PulseStimulus(double delayTime, double pulseWidth, double period)
    {
        DelayTime = delayTime;
        PulseWidth = pulseWidth;
        Period = period;
    }

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
    protected override string GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId) =>
        $"V{Util.GetSpiceName(uniqueId, 0, "pulse")} {signal.GetSpiceName()} 0 PULSE(0 {Util.VDD} {DelayTime} {Util.RiseFall} {Util.RiseFall} {PulseWidth} {Period})";

    /// <inheritdoc/>
    protected override IEnumerable<IEntity> GetSpiceSharpEntitiesGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId)
    {
        Pulse pulse = new(0, Util.VDD, DelayTime, Util.RiseFall, Util.RiseFall, PulseWidth, Period);
        yield return new VoltageSource($"V{Util.GetSpiceName(uniqueId, 0, "pulse")}", signal.GetSpiceName(), "0", pulse);
    }
}