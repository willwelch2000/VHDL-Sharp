using VHDLSharp.Dimensions;
using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Single-node (one-dimensional) stimulus
/// </summary>
public abstract class Stimulus : IStimulus
{
    /// <inheritdoc/>
    public DefiniteDimension Dimension { get; } = new(1);

    /// <inheritdoc/>
    public IEnumerable<Stimulus> Stimuli => [this];

    /// <summary>
    /// Convert to Spice given single-node signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    protected abstract string ToSpiceGivenSingleNodeSignal(SingleNodeNamedSignal signal, string uniqueId); 

    /// <summary>
    /// Convert to Spice given signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public string ToSpice(NamedSignal signal, string uniqueId)
    {
        if (signal.Dimension.NonNullValue == 1)
            return ToSpiceGivenSingleNodeSignal(signal.ToSingleNodeNamedSignals.First(), uniqueId);
            
        throw new Exception("Input signal must have dimension of 1");
    }
}