using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class for a set of stimuli to be applied on a <see cref="Port"/>
/// </summary>
public interface IStimulusSet
{
    /// <summary>
    /// Dimension of stimulus
    /// </summary>
    public DefiniteDimension Dimension { get; }

    /// <summary>
    /// All individual (single-node) stimuli
    /// Must be of length <see cref="Dimension"/>
    /// </summary>
    public IEnumerable<Stimulus> Stimuli { get; }

    /// <summary>
    /// Convert to Spice given signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public string ToSpice(NamedSignal signal, string uniqueId); 

    /// <summary>
    /// Convert to Spice# entities that can be added to a simulation circuit
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public IEnumerable<IEntity> ToSpiceSharpEntities(NamedSignal signal, string uniqueId);
}