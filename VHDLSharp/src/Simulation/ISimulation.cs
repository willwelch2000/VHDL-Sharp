using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Interface for anything that can simulate a <see cref="IModule"/> with applied stimuli
/// </summary>
public interface ISimulation
{
    /// <summary>
    /// Mapping of module's ports to stimuli
    /// </summary>
    public StimulusMapping StimulusMapping { get; }

    /// <summary>
    /// Module that has stimuli applied
    /// </summary>
    public IModule Module { get; }

    /// <summary>
    /// List of signals to receive output for
    /// </summary>
    public ICollection<SignalReference> SignalsToMonitor { get; }

    /// <summary>
    /// Perform the simulation
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ISimulationResult> Simulate();
}