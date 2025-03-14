using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Stimulus that applies a digital value based on specified timing
/// </summary>
public class TimeDefinedStimulus : Stimulus
{
    /// <summary>
    /// Mapping of times to digital values
    /// </summary>
    public Dictionary<double, bool> Points { get; } = [];

    /// <inheritdoc/>
    protected override string GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId)
    {
        string toReturn = $"V{SpiceUtil.GetSpiceName(uniqueId, 0, "pulse")} {signal.GetSpiceName()} 0 PWL(";

        // Get points as (time, val) ordered by time
        List<(double time, bool val)> orderedPoints = [.. Points.Select<KeyValuePair<double, bool>, (double time, bool val)>(p => (p.Key, p.Value)).OrderBy(p => p.time)];

        // Uses first value as starting value--I think SPICE uses the first given value as starting value
        bool prevVal = orderedPoints.First().val;
        bool firstLoop = true;
        foreach ((double time, bool val) in orderedPoints)
        {
            // Skip if same as previous
            if (val == prevVal && !firstLoop)
                continue;

            // Add space if not first time
            if (!firstLoop)
                toReturn += ' ';

            // Add point at time step with previous value
            toReturn += $"{time:G5} {GetVoltage(prevVal)} ";

            // Add point right after with new value
            toReturn += $"{time + Util.RiseFall:G7} {GetVoltage(val)}";

            firstLoop = false;
        }
        toReturn += ")";

        return toReturn;
    }

    private static double GetVoltage(bool input) => input ? SpiceUtil.VDD : 0;

    /// <inheritdoc/>
    protected override IEnumerable<IEntity> GetSpiceSharpEntitiesGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId)
    {
        // Get points as (time, val) ordered by time
        List<(double time, bool val)> orderedPoints = [.. Points.Select<KeyValuePair<double, bool>, (double time, bool val)>(p => (p.Key, p.Value)).OrderBy(p => p.time)];

        // Uses first value as starting value
        bool prevVal = orderedPoints.First().val;
        bool firstLoop = true;
        List<double> vector = [];
        foreach ((double time, bool val) in orderedPoints)
        {
            // Skip if same as previous
            if (val == prevVal && !firstLoop)
                continue;

            // Add point at time step with previous value
            vector.Add(time);
            vector.Add(GetVoltage(prevVal));

            // Add point right after with new value
            vector.Add(time + Util.RiseFall);
            vector.Add(GetVoltage(val));
        }

        Pwl pwl = new();
        pwl.SetPoints([.. vector]);
        yield return new VoltageSource($"V{SpiceUtil.GetSpiceName(uniqueId, 0, "pwl")}", signal.GetSpiceName(), "0", pwl);
    }
}