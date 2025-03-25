using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Contains all info regarding the current state of a <see cref="RuleBasedSimulation"/>
/// </summary>
public class RuleBasedSimulationState
{
    private Dictionary<ISingleNodeNamedSignal, List<bool>> signalValues = [];

    /// <summary>
    /// Current time step
    /// </summary>
    public double TimeStep { get; internal set; }
}