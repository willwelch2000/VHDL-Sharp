using VHDLSharp.Dimensions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;

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
    /// All individual (single-node) stimuli. 
    /// Must be of length <see cref="Dimension"/>
    /// </summary>
    public IEnumerable<Stimulus> Stimuli { get; }

    /// <summary>
    /// Convert to Spice given signal and unique id
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public SpiceCircuit GetSpice(INamedSignal signal, string uniqueId);
}