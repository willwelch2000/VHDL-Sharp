namespace VHDLSharp.Simulations;

/// <summary>
/// Delegate method that produces event times given the simulation length
/// </summary>
/// <param name="simulationLength">Length of simulation, given so rule knows how long to generate values for</param>
/// <returns></returns>
public delegate IEnumerable<double> TimeGenerator(double simulationLength);

/// <summary>
/// Delegate method that produces an integer value for an output signal 
/// given the current simulation state
/// </summary>
/// <param name="state">Current simulation state</param>
/// <returns></returns>
public delegate int ValueCalculation(RuleBasedSimulationState state);

/// <summary>
/// A rule that makes up a rule-based simulation
/// </summary>>
/// <param name="outputSignal">Output signal controlled by this rule</param>
/// <param name="outputValueCalculation">Method for getting value of output signal given the current simulation state</param>
public class SimulationRule(SignalReference outputSignal, ValueCalculation outputValueCalculation)
{
    /// <summary>
    /// Output signal controlled by this rule
    /// </summary>
    public SignalReference OutputSignal { get; } = outputSignal;

    /// <summary>
    /// Function to generate the times at which this rule independently changes its output values,
    /// given the simulation length.
    /// By default, this produces an empty list
    /// </summary>
    public TimeGenerator IndependentEventTimeGenerator { get; init; } = length => [];

    /// <summary>
    /// Method for getting value of output signal given the current simulation state
    /// </summary>
    /// <returns></returns>
    public ValueCalculation OutputValueCalculation { get; } = outputValueCalculation;

    /// <summary>
    /// Tests if rules have any overlapping output signals
    /// </summary>
    /// <param name="rules"></param>
    /// <returns></returns>
    internal static bool RulesOverlap(IEnumerable<SimulationRule> rules)
    {
        HashSet<SignalReference> singleNodeAscendedOutputs = [];
        foreach (SimulationRule rule in rules)
        {
            // Try to add the output signal's (ascended) child signals, return true if duplicates exist
            SignalReference outputSignal = rule.OutputSignal;
            IEnumerable<SignalReference> singleNodeOutputSignals = outputSignal.Signal.ToSingleNodeSignals.Select(outputSignal.Submodule.GetChildSignalReference);
            foreach (SignalReference singleNodeOutputSignal in singleNodeOutputSignals)
                if (!singleNodeAscendedOutputs.Add(singleNodeOutputSignal.Ascend()))
                    return true;
        }
        return false;
    }
}