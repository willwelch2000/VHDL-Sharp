
using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a simulation setup
/// </summary>
/// <param name="module">Module that is simulated</param>
public class SimulationSetup(Module module)
{
    /// <summary>
    /// Mapping of module's ports to stimuli
    /// </summary>
    public StimulusMapping StimulusMapping { get; } = new(module);

    /// <summary>
    /// Module that has stimuli applied
    /// </summary>
    public Module Module { get; } = module;

    /// <summary>
    /// Assign a stimulus to a portx
    /// </summary>
    /// <param name="port"></param>
    /// <param name="stimulus"></param>
    public void AssignStimulus(Port port, IStimulus stimulus) => StimulusMapping[port] = stimulus;
}