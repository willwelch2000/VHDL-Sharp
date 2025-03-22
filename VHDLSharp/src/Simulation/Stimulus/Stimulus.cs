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
            
        throw new Exception("Input signal must have dimension of 1");
    }

    /// <summary>
    /// Convert to Spice given single-node signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    protected abstract SpiceCircuit GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId); 
}