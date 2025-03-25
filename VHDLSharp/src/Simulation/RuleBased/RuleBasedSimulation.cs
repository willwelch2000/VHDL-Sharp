using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a rule-based simulation of a <see cref="IModule"/>, using a custom simulation scheme
/// </summary>
/// <param name="module"></param>
public class RuleBasedSimulation(IModule module) : Simulation(module)
{
    public ITimeStepGenerator TimeStepGenerator { get; set; }

    /// <inheritdoc/>
    public override IEnumerable<ISimulationResult> Simulate()
    {
        throw new NotImplementedException();
    }
}