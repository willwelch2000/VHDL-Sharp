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
        if (!ValidityManager.IsValid(out Exception? issue))
            throw new InvalidException("Simulation setup must be valid to convert to Spice# circuit", issue);
        if (!IsComplete(out string? reason))
            throw new IncompleteException($"Simulation setup must be complete to convert to circuit: {reason}");

        SubmoduleReference topLevelSubmodule = new(Module, []);
        return Module.GetSimulationRules()
        .Concat(StimulusMapping.Select(kvp => kvp.Value.GetSimulationRule(new(topLevelSubmodule, kvp.Key.Signal))));
    }

    /// <inheritdoc/>
    protected override IEnumerable<ISimulationResult> SimulateWithoutCheck()
    {
        SimulationRule[] rules = [.. GetSimulationRules()];
        Dictionary<SignalReference, SignalReference> redirects = GetRedirectDictionary(rules);
        RuleBasedSimulationState state = new()
        {
            Redirects = redirects,
        };
        double[] independentEventTimes = [.. rules.SelectMany(r => r.IndependentEventTimeGenerator(Length)).Order()];
        if (SimulationRule.RulesOverlap(rules))
            throw new Exception("Rules have overlapping output signals");

        Queue<double> nextTimeSteps = [];
        while (true)
        {
            // Apply rules
            foreach (SimulationRule rule in rules)
                state.AddSignalValue(rule.OutputSignal, rule.OutputValueCalculation(state));
            
            // Go to next time step, if within length
            if (nextTimeSteps.Count == 0)
                nextTimeSteps = new(timeStepGenerator.NextTimeSteps(state, independentEventTimes, Length));
            double nextTimeStep = nextTimeSteps.Dequeue();
            if (nextTimeStep > Length)
                break;
            state.CurrentTimeStep = nextTimeStep;
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

    // Generates "redirect dictionary," which maps signals to the signal that they ultimately mirror
    // Follows several steps, not just first
    // 1->2, 2->3, 4->5 becomes 1->3, 2->3, 4->5
    private static Dictionary<SignalReference, SignalReference> GetRedirectDictionary(SimulationRule[] rules)
    {
        // Dictionary mapping signal to the next-step redirect
        Dictionary<SignalReference, SignalReference> oneStepRedirects = rules.Where(r => r.Redirect is not null)
            .SelectMany(r => r.OutputSignal.GetSingleNodeReferences().Zip(r.Redirect!.GetSingleNodeReferences())).ToDictionary();

        // New dictionary that follows the steps to the end
        Dictionary<SignalReference, SignalReference> finalRedirects = [];
        foreach (SignalReference signal in oneStepRedirects.Keys)
        {
            HashSet<SignalReference> steps = [signal];
            SignalReference currentStep = signal;
            while (oneStepRedirects.TryGetValue(currentStep, out SignalReference? next))
            {
                if (!steps.Add(next))
                    throw new Exception("Circular redirects detected");
                currentStep = next;
            }
            finalRedirects[signal] = currentStep;
        }

        return finalRedirects;
    }
}