namespace VHDLSharp.Simulations;

/// <summary>
/// Default time step generator for a rule-based simulation
/// </summary>
public class DefaultTimeStepGenerator : ITimeStepGenerator
{
    /// <summary>
    /// Minimum time step that will be used
    /// </summary>
    public double MinTimeStep { get; set; } = 1e-6;

    /// <summary>
    /// Maximum time step that will be used. Null means no limit
    /// </summary>
    public double? MaxTimeStep { get; set; } = null;

    /// <inheritdoc/>
    public IEnumerable<double> NextTimeSteps(RuleBasedSimulationState state, double[] independentEventTimes, double simulationLength)
    {
        // If state (any signal) has changed between the last two timesteps, move min time step
        if (state.AllSingleNodeSignals
            .Select(state.GetSingleNodeSignalValues)
            .Any(vals => vals.Count < 2 || vals.TakeLast(2).Distinct().Count() > 1))
        {
            yield return state.CurrentTimeStep + MinTimeStep;
            yield break;
        }
        // Otherwise, go to next independent event time
        double nextIndependentTimeStep = independentEventTimes.FirstOrDefault(time => time > state.CurrentTimeStep, simulationLength);

        // If moving max step would be before the next independent time step - min step, move that max time step
        double nextIndependentMinusMinStep = nextIndependentTimeStep - MinTimeStep;
        double nextIndependentPlusMinStep = nextIndependentTimeStep + MinTimeStep;
        if (MaxTimeStep is not null)
        {
            double nextStepWithMax = state.CurrentTimeStep + MaxTimeStep.Value;
            if (nextStepWithMax < nextIndependentMinusMinStep)
            {
                yield return nextStepWithMax;
                yield break;
            }
        }

        // Otherwise, return points around the next independent time step
        if (nextIndependentMinusMinStep > state.CurrentTimeStep)
            yield return nextIndependentMinusMinStep;
        yield return nextIndependentTimeStep;
        if(nextIndependentPlusMinStep > simulationLength)
            yield return nextIndependentPlusMinStep;
    }
}