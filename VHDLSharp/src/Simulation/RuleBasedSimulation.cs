using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a rule-based simulation of a <see cref="IModule"/>, using a custom simulation scheme
/// </summary>
/// <param name="module"></param>
public class RuleBasedSimulation(IModule module) : Simulation(module)
{
    /// <inheritdoc/>
    public override IEnumerable<SimulationResult> Simulate()
    {
        throw new NotImplementedException();
    }
}