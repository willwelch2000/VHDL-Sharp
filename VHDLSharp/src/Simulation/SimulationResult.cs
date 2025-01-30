using SpiceSharp.Simulations;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class used as one of many results of a simulation
/// </summary>
public class SimulationResult
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
    internal SimulationResult(SignalReference signalReference, Transient simulation)
    {
        SignalReference = signalReference;
        this.simulation = simulation;
        exports = [.. signalReference.GetSpiceSharpReferences().Select(r => new RealVoltageExport(simulation, r))];
    }

    /// <summary>
    /// Reference to signal that is monitored
    /// </summary>
    public SignalReference SignalReference { get; }

    /// <summary>
    /// X values of result--time steps
    /// </summary>
    public double[] TimeSteps => [.. timeSteps];

    /// <summary>
    /// Digital values of signal
    /// </summary>
    public int[] Values => [.. values];

    /// <summary>
    /// Time steps paired with digital values
    /// </summary>
    public IEnumerable<(double, int)> TimeStepsAndValues => TimeSteps.Zip(Values);

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
            if (exports[i].Value > Util.VDD/2)
                value += 1<<i;

        values.Add(value);
    }
}