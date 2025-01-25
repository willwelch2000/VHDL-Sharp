using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class used as one of many results of a simulation
/// </summary>
public class SimulationResult(SignalReference signal)
{
    private readonly List<double> timeSteps = [];

    private readonly List<bool> values = [];

    /// <summary>
    /// Reference to signal that is monitored
    /// </summary>
    public SignalReference SignalReference { get; } = signal;

    /// <summary>
    /// X values of result--time steps
    /// </summary>
    public double[] TimeSteps => [.. timeSteps];

    /// <summary>
    /// Digital values of signal
    /// </summary>
    public bool[] Values => [.. values];

    /// <summary>
    /// Time steps paired with digital values
    /// </summary>
    public IEnumerable<(double, bool)> TimeStepsAndValues => TimeSteps.Zip(Values);

    internal void AddTimeStepValue(double time, double voltage)
    {
        timeSteps.Add(time);
        values.Add(voltage > Util.VDD/2);
    }
}