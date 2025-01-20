using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Multi-dimensional stimulus set
/// Made up of multiple <see cref="Stimulus"/> objects
/// </summary>
public class MultiDimensionalStimulus : IStimulusSet
{
    /// <summary>
    /// Default constructor
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
    public string ToSpice(NamedSignal signal, string uniqueId)
    {
        if (!signal.Dimension.Compatible(Dimension))
            throw new Exception("Signal must be compatible with stimulus dimension");
            
        string toReturn = "";
        SingleNodeNamedSignal[] signals = [.. signal.ToSingleNodeNamedSignals];
        for (int i = 0; i < Stimuli.Count; i++)
            toReturn += $"{Stimuli[i].ToSpice(signals[i], $"uniqueId_{i}")}\n";

        return toReturn;
    }

    /// <inheritdoc/>
    public IEnumerable<IEntity> ToSpiceSharpEntities(NamedSignal signal, string uniqueId)
    {
        throw new NotImplementedException();
    }
}