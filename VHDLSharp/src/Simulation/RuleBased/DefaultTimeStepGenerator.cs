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

    /// <inheritdoc/>
    public double NextTimeStep(RuleBasedSimulationState state, double[] independentEventTimes, double simulationLength)
    {
        // If state (any signal) has changed between the last two timesteps, move min time step
        if (state.AllSingleNodeSignals
            .Select(state.GetSingleNodeSignalValues)
            .Any(vals => vals.Count < 2 || vals.TakeLast(2).Distinct().Count() > 1))
            return state.CurrentTimeStep + MinTimeStep;

        // Otherwise, go to next independent event time
        return independentEventTimes.FirstOrDefault(time => time > state.CurrentTimeStep, simulationLength);
    }
}