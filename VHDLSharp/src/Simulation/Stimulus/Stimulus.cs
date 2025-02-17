using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Signals;

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
    public string GetSpice(INamedSignal signal, string uniqueId)
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
    protected abstract string GetSpiceGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId); 

    /// <inheritdoc/>
    public IEnumerable<IEntity> GetSpiceSharpEntities(INamedSignal signal, string uniqueId)
    {
        if (signal.Dimension.NonNullValue == 1)
            return GetSpiceSharpEntitiesGivenSingleNodeSignal(signal.ToSingleNodeSignals.First(), uniqueId);
            
        throw new Exception("Input signal must have dimension of 1");
    }

    /// <summary>
    /// Convert to Spice# entities given single-node signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    protected abstract IEnumerable<IEntity> GetSpiceSharpEntitiesGivenSingleNodeSignal(ISingleNodeNamedSignal signal, string uniqueId);
}