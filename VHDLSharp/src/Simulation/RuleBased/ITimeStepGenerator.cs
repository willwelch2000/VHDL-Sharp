namespace VHDLSharp.Simulations;

/// <summary>
/// Interface for any class that can be used to generate timesteps in a <see cref="RuleBasedSimulation"/>
/// </summary>
public interface ITimeStepGenerator
{
    /// <summary>
    /// Get the next time step, given the current state and rules
    /// </summary>
    /// <param name="state"></param>
    /// <param name="rules"></param>
    /// <returns></returns>
    public double NextTimeStep(RuleBasedSimulationState state, IEnumerable<SimulationRule> rules);
}