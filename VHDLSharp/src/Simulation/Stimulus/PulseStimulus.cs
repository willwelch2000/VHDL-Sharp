using SpiceSharp.Components;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;
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
    protected override SpiceCircuit GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId)
    {
        Pulse pulse = new(0, SpiceUtil.VDD, DelayTime, Util.RiseFall, Util.RiseFall, PulseWidth, Period);
        VoltageSource source = new(SpiceUtil.GetSpiceName(uniqueId, 0, "pulse"), signal.GetSpiceName(), "0", pulse);
        return new([source]);
    }

    /// <inheritdoc/>
    protected override bool GetValue(double currentTime)
    {
        if (currentTime < DelayTime)
            return false;

        // Shift time based on delay time and period, to compare to width
        double shiftedTime = (currentTime - DelayTime) % Period;
        if (shiftedTime < 0)
            shiftedTime += Period;

        return shiftedTime < PulseWidth;
    }
}