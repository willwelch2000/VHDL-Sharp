using VHDLSharp.Dimensions;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Simulations;

/// <summary>
/// Single-node (one-dimensional) stimulus set
/// </summary>
public abstract class Stimulus : IStimulusSet
{
    /// <inheritdoc/>
    public DefiniteDimension Dimension { get; } = new(1);

    /// <inheritdoc/>
    public IEnumerable<Stimulus> Stimuli => [this];

    /// <summary>
    /// Convert to Spice given signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public SpiceCircuit GetSpice(INamedSignal signal, string uniqueId)
    {
        if (signal.Dimension.NonNullValue == 1)
            return GetSpiceGivenSingleNodeSignal(signal.ToSingleNodeSignals.First(), uniqueId);
            
        throw new Exception("Attached signal must have dimension of 1");
    }
    
    /// <summary>
    /// Get singular simulation rule for a given output signal reference
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public SimulationRule GetSimulationRule(SignalReference signal)
    {
        if (signal.Signal.Dimension.NonNullValue == 1)
            return new(signal, (state) => GetValue(state.CurrentTimeStep) ? 1 : 0)
            {
                IndependentEventTimeGenerator = GetIndependentEventTimes
            };
            
        throw new Exception("Attached signal must have dimension of 1");
    }

    /// <summary>
    /// Convert to Spice given single-node signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    protected abstract SpiceCircuit GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId); 

    /// <summary>
    /// Get value of stimulus at a given time
    /// </summary>
    /// <param name="currentTime"></param>
    /// <returns></returns>
    protected abstract bool GetValue(double currentTime);

    /// <summary>
    /// Get times at which the stimulus spontaneously changes given length of simulation
    /// </summary>
    /// <param name="simulationLength">Length of simulation</param>
    /// <returns>List of times at which the stimulus changes spontaneously</returns>
    protected abstract IEnumerable<double> GetIndependentEventTimes(double simulationLength);
}