using SpiceSharp.Simulations;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class used as one of many results of a Spice simulation
/// TODO convert to interface, this will be SpiceSimulationResult
/// </summary>
public class SpiceSimulationResult : ISimulationResult
{
    private readonly List<double> timeSteps = [];

    private readonly List<int> values = [];

    private readonly RealVoltageExport[] exports;

    private readonly Transient simulation;

    /// <summary>
    /// Constructor given signal and simulation
    /// </summary>
    /// <param name="signalReference">Signal being monitored</param>
    /// <param name="simulation">Simulation producing results</param>
    internal SpiceSimulationResult(SignalReference signalReference, Transient simulation)
    {
        SignalReference = signalReference;
        this.simulation = simulation;
        exports = [.. signalReference.GetSpiceSharpReferences().Select(r => new RealVoltageExport(simulation, r))];
    }

    /// <inheritdoc/>
    public SignalReference SignalReference { get; }

    /// <inheritdoc/>
    public double[] TimeSteps => [.. timeSteps];

    /// <inheritdoc/>
    public int[] Values => [.. values];

    /// <summary>
    /// Call this method after each simulation step to add the values from the voltage export objects
    /// </summary>
    internal void AddCurrentTimeStepValue()
    {
        // Get time from simulation
        timeSteps.Add(simulation.Time);

        // Calculate value--each export is a bit in the value
        int value = 0;
        for (int i = 0; i < exports.Length; i++)
            if (exports[i].Value > SpiceUtil.VDD/2)
                value += 1<<i;

        values.Add(value);
    }
}