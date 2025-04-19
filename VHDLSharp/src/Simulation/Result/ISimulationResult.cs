namespace VHDLSharp.Simulations;

/// <summary>
/// Interface for result of a <see cref="ISimulation"/>
/// </summary>
public interface ISimulationResult
{
    /// <summary>
    /// Reference to signal that is monitored
    /// </summary>
    public SignalReference SignalReference { get; }

    /// <summary>
    /// X values of result--time steps
    /// </summary>
    public double[] TimeSteps { get; }

    /// <summary>
    /// Digital values of signal
    /// </summary>
    public int[] Values { get; }

    /// <summary>
    /// Time steps paired with digital values
    /// </summary>
    public IEnumerable<(double, int)> TimeStepsAndValues => TimeSteps.Zip(Values);
}