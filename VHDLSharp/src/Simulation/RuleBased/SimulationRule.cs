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
public class SimulationRule
{
    /// <summary>
    /// <see cref="SimulationRule"/> given an output signal and the method for calculating its value
    /// </summary>
    /// <param name="outputSignal">Output signal controlled by this rule</param>
    /// <param name="outputValueCalculation">Method for getting value of output signal given the current simulation state</param>
    public SimulationRule(SignalReference outputSignal, ValueCalculation outputValueCalculation)
    {
        OutputSignal = outputSignal;
        OutputValueCalculation = outputValueCalculation;
    }

    /// <summary>
    /// <see cref="SimulationRule"/> given an output signal and a <see cref="SimulationRule"/> to redirect to
    /// </summary>
    /// <param name="outputSignal"></param>
    /// <param name="redirect"></param>
    public SimulationRule(SignalReference outputSignal, SignalReference redirect)
    {
        OutputSignal = outputSignal;
        Redirect = redirect;
        if (outputSignal.Signal.Dimension.NonNullValue != redirect.Signal.Dimension.NonNullValue)
            throw new Exception("Output signal and redirect signal must have the same dimension");
        // Just add default value calculation--not used
        OutputValueCalculation = state => 0;
    }

    /// <summary>
    /// Output signal controlled by this rule
    /// </summary>
    public SignalReference OutputSignal { get; }

    /// <summary>
    /// Function to generate the times at which this rule independently changes its output values,
    /// given the simulation length.
    /// By default, this produces an empty list
    /// </summary>
    public TimeGenerator IndependentEventTimeGenerator { get; init; } = length => [];

    /// <summary>
    /// Optionally, another <see cref="SignalReference"/> that this should mirror.
    /// If given, this is used instead of the <see cref="OutputValueCalculation"/>
    /// </summary>
    public SignalReference? Redirect { get; }

    /// <summary>
    /// Method for getting value of output signal given the current simulation state.
    /// If <see cref="Redirect"/> is specified, this is not used
    /// </summary>
    /// <returns></returns>
    public ValueCalculation OutputValueCalculation { get; }

    /// <summary>
    /// Tests if rules have any overlapping output signals
    /// </summary>
    /// <param name="rules"></param>
    /// <returns></returns>
    internal static bool RulesOverlap(IEnumerable<SimulationRule> rules)
    {
        HashSet<SignalReference> singleNodeOutputs = [];
        foreach (SimulationRule rule in rules)
        {
            // Try to add the output signal's child (single-node) signals, return true if duplicates exist
            SignalReference outputSignal = rule.OutputSignal;
            IEnumerable<SignalReference> singleNodeOutputSignals = outputSignal.Signal.ToSingleNodeSignals.Select(outputSignal.Submodule.GetChildSignalReference);
            foreach (SignalReference singleNodeOutputSignal in singleNodeOutputSignals)
                if (!singleNodeOutputs.Add(singleNodeOutputSignal))
                    return true;
        }
        return false;
    }
}