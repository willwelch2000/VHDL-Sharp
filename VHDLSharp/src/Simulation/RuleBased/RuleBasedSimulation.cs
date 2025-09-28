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
            throw new InvalidException("Simulation setup must be valid to convert to Spice# circuit", ValidityManager.Issues().First().Exception);
        if (!IsComplete(out string? reason))
            throw new IncompleteException($"Simulation setup must be complete to convert to circuit: {reason}");

        SubcircuitReference topLevelSubcircuit = new(Module, []);
        return Module.GetSimulationRules()
        .Concat(StimulusMapping.Select(kvp => kvp.Value.GetSimulationRule(new(topLevelSubcircuit, kvp.Key.Signal))));
    }

    /// <inheritdoc/>
    protected override IEnumerable<ISimulationResult> SimulateWithoutCheck()
    {
        SimulationRule[] rules = [.. GetSimulationRules()];
        RuleBasedSimulationState state = new();
        double[] independentEventTimes = [.. rules.SelectMany(r => r.IndependentEventTimeGenerator(Length)).Order()];
        if (SimulationRule.RulesOverlap(rules))
            throw new Exception("Rules have overlapping output signals");

        Queue<double> nextTimeSteps = [];
        while (state.CurrentTimeStep <= Length)
        {
            // Apply rules
            foreach (SimulationRule rule in rules)
                state.AddSignalValue(rule.OutputSignal, rule.OutputValueCalculation(state));
            
            // Go to next time step
            if (nextTimeSteps.Count == 0)
                nextTimeSteps = new(timeStepGenerator.NextTimeSteps(state, independentEventTimes, Length));
            state.CurrentTimeStep = nextTimeSteps.Dequeue();
        }

        // Get results
        foreach (SignalReference signal in SignalsToMonitor)
        {
            yield return new RuleBasedSimulationResult(signal)
            {
                TimeSteps = [.. state.AllTimeSteps],
                Values = [.. state.GetSignalValues(signal)],
            };
        }
    }
}