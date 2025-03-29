namespace VHDLSharp.Simulations;

/// <summary>
/// Interface for any class that can be used to generate timesteps in a <see cref="RuleBasedSimulation"/>
/// </summary>
public interface ITimeStepGenerator
{
    /// <summary>
    /// Get the next time step, given the current state and independent event times
    /// </summary>
    /// <param name="state">State of the current simulation</param>
    /// <param name="independentEventTimes">Times at which rules initiate a change</param>
    /// <param name="simulationLength">Total length of the simulation</param>
    /// <returns></returns>
    public double NextTimeStep(RuleBasedSimulationState state, double[] independentEventTimes, double simulationLength);
}