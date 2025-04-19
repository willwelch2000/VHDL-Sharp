namespace VHDLSharp.Simulations;

/// <summary>
/// Class used as one of many results of a rule-based simulation
/// </summary>
/// <param name="signal">Signal this relates to</param>
public class RuleBasedSimulationResult(SignalReference signal) : ISimulationResult
{
    /// <inheritdoc/>
    public SignalReference SignalReference { get; } = signal;

    /// <inheritdoc/>
    public double[] TimeSteps { get; internal set; } = [];

    /// <inheritdoc/>
    public int[] Values { get; internal set; } = [];
}