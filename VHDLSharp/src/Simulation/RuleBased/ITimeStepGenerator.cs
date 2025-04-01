namespace VHDLSharp.Simulations;

/// <summary>
/// Interface for any class that can be used to generate timesteps in a <see cref="RuleBasedSimulation"/>
/// </summary>
public interface ITimeStepGenerator
{
    /// <summary>
    /// Get the next time step, or multiple next time steps, given the current state and independent event times.
    /// If multiple are given, they will be done in series before requesting more. 
    /// </summary>
    /// <param name="state">State of the current simulation</param>
    /// <param name="independentEventTimes">Times at which rules initiate a change</param>
    /// <param name="simulationLength">Total length of the simulation</param>
    /// <returns></returns>
    public IEnumerable<double> NextTimeSteps(RuleBasedSimulationState state, double[] independentEventTimes, double simulationLength);
}