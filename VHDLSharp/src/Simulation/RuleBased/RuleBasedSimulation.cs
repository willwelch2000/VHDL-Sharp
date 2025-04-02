using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Validation;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a rule-based simulation of a <see cref="IModule"/>, using a custom simulation strategy
/// </summary>
/// <param name="module"></param>
/// <param name="timeStepGenerator"></param>
public class RuleBasedSimulation(IModule module, ITimeStepGenerator timeStepGenerator) : Simulation(module)
{
    private ITimeStepGenerator timeStepGenerator = timeStepGenerator;

    /// <summary>
    /// Object used to generate the time steps for the simulation
    /// </summary>
    public ITimeStepGenerator TimeStepGenerator
    {
        get => timeStepGenerator;
        set
        {
            updated?.Invoke(this, EventArgs.Empty);
            timeStepGenerator = value;
        }
    }

    /// <summary>
    /// Get all simulation rules for the setup
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SimulationRule> GetSimulationRules()
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Simulation setup must be valid to convert to Spice# circuit");
        if (!IsComplete())
            throw new IncompleteException("Simulation setup must be complete to convert to circuit");

        SubcircuitReference topLevelSubcircuit = new(Module, []);
        return Module.GetSimulationRules()
        .Concat(StimulusMapping.SelectMany(kvp => kvp.Value.GetSimulationRules(new(topLevelSubcircuit, kvp.Key.Signal))));
    }

    /// <inheritdoc/>
    protected override IEnumerable<ISimulationResult> SimulateWithoutCheck()
    {
        // Get all rules
        SimulationRule[] rules = [.. GetSimulationRules()];

        RuleBasedSimulationState state = new();

        while (state.CurrentTimeStep <= Length)
        {
            
        }

        throw new NotImplementedException();
    }

    // TODO make sure that every signal is covered exactly once
}