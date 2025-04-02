using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a rule-based simulation of a <see cref="IModule"/>, using a custom simulation strategy
/// </summary>
/// <param name="module"></param>
/// <param name="timeStepGenerator"></param>
public class RuleBasedSimulation(IModule module, ITimeStepGenerator timeStepGenerator) : Simulation(module)
{
    /// <summary>
    /// Object used to generate the time steps for the simulation
    /// </summary>
    public ITimeStepGenerator TimeStepGenerator { get; set; } = timeStepGenerator;

    /// <summary>
    /// Get all simulation rules for the setup
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SimulationRule> GetSimulationRules()
    {
        SubcircuitReference topLevelSubcircuit = new(Module, []);
        return Module.GetSimulationRules()
        .Concat(StimulusMapping.SelectMany(kvp => kvp.Value.GetSimulationRules(new(topLevelSubcircuit, kvp.Key.Signal))));
    }

    /// <inheritdoc/>
    protected override IEnumerable<ISimulationResult> SimulateWithoutCheck()
    {
        // Get all rules
        SimulationRule[] rules = [.. GetSimulationRules()];

        throw new NotImplementedException();
    }

    // TODO make sure that every signal is covered exactly once
}