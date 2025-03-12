using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Multi-dimensional stimulus set. 
/// Made up of multiple <see cref="Stimulus"/> objects
/// </summary>
public class MultiDimensionalStimulus : IStimulusSet
{
    /// <summary>
    /// Default constructor
    /// TODO might should get rid of this and make Stimuli readonly, so that MultiDimensionalConstantStimulus works better
    /// </summary>
    public MultiDimensionalStimulus() {}

    /// <summary>
    /// Constructor with input <see cref="IEnumerable{Stimulus}"/>
    /// </summary>
    /// <param name="stimuli"></param>
    public MultiDimensionalStimulus(IEnumerable<Stimulus> stimuli)
    {
        Stimuli = [.. stimuli];
    }

    /// <inheritdoc/>
    public DefiniteDimension Dimension => new(Stimuli.Count);

    /// <summary>
    /// All stimuli in type <see cref="List{Stimuli}"/>
    /// </summary>
    public List<Stimulus> Stimuli { get; } = [];

    IEnumerable<Stimulus> IStimulusSet.Stimuli => Stimuli;

    /// <inheritdoc/>
    public string GetSpice(INamedSignal signal, string uniqueId)
    {
        if (!signal.Dimension.Compatible(Dimension))
            throw new Exception("Signal must be compatible with stimulus dimension");
            
        string toReturn = "";
        ISingleNodeNamedSignal[] signals = [.. signal.ToSingleNodeSignals];

        // Pair each stimulus with corresponding signal
        for (int i = 0; i < Stimuli.Count; i++)
            toReturn += $"{Stimuli[i].GetSpice(signals[i], $"{uniqueId}_{i}")}\n";

        return toReturn;
    }

    /// <inheritdoc/>
    public IEnumerable<IEntity> GetSpiceSharpEntities(INamedSignal signal, string uniqueId)
    {
        if (!signal.Dimension.Compatible(Dimension))
            throw new Exception("Signal must be compatible with stimulus dimension");
            
        ISingleNodeNamedSignal[] signals = [.. signal.ToSingleNodeSignals];
        
        // Pair each stimulus with corresponding signal
        for (int i = 0; i < Stimuli.Count; i++)
            foreach (IEntity entity in Stimuli[i].GetSpiceSharpEntities(signals[i], $"{uniqueId}_{i}"))
                yield return entity;
    }
}