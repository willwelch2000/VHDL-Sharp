using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Contains all info regarding the current state of a <see cref="RuleBasedSimulation"/>
/// </summary>
public class RuleBasedSimulationState
{
    private readonly Dictionary<SignalReference, List<bool>> singleNodeSignalValues = [];

    private readonly List<double> allTimeSteps = [0];

    private double currentTimeStep = 0;

    /// <summary>
    /// Current time step
    /// </summary>
    public double CurrentTimeStep
    {
        get => currentTimeStep;
        set
        {
            if (!singleNodeSignalValues.Values.All(l => l.Count == allTimeSteps.Count))
                throw new Exception("All signals should have a value assigned before incrementing time step");
            currentTimeStep = value;
            allTimeSteps.Add(value);
        }
    }

    /// <summary>
    /// List of all timesteps, including the current one
    /// </summary>
    public List<double> AllTimeSteps => [.. allTimeSteps];

    /// <summary>
    /// Get values for a signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public List<int> this[SignalReference signal]
    {
        get => GetSignalValues(signal);
    }

    /// <summary>
    /// Get values as booleans for a single-node signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<bool> GetSingleNodeSignalValues(SignalReference signal) => signal.Signal switch
    {
        ISingleNodeNamedSignal => [.. singleNodeSignalValues[signal]],
        _ => throw new Exception("Signal reference must be to a single-node signal")
    };

    /// <summary>
    /// Get values for a signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<int> GetSignalValues(SignalReference signal)
    {
        try
        {
            if (signal.Signal is ISingleNodeNamedSignal)
                return [.. singleNodeSignalValues[signal].Select(val => val ? 1 : 0)];

            SubcircuitReference subcircuit = signal.Subcircuit;
            SignalReference[] singleNodeSignals = [.. signal.Signal.ToSingleNodeSignals.Select(subcircuit.GetChildSignalReference)];
            List<int> values = [];
            for (int i = 0; i < singleNodeSignalValues[singleNodeSignals[0]].Count; i++)
                values.Add(singleNodeSignals.Select(s => singleNodeSignalValues[s][i] ? 1 : 0).Sum());

            return values;
        }
        catch (KeyNotFoundException keyEx)
        {
            throw new Exception($"Signal not added to simulation state", keyEx);
        }
        catch (IndexOutOfRangeException indexEx)
        {
            throw new Exception($"Child signals not same length", indexEx);
        }
    }

    /// <summary>
    /// Add a single-node signal value as a boolean
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="value"></param>
    internal void AddSignalValue(SignalReference signal, bool value)
    {
        if (singleNodeSignalValues.TryGetValue(signal, out List<bool>? values))
            values.Add(value);
        else
            singleNodeSignalValues[signal] = [value];
    }

    /// <summary>
    /// Add a signal value
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="value"></param>
    internal void AddSignalValue(SignalReference signal, int value)
    {
        SubcircuitReference subcircuit = signal.Subcircuit;
        foreach ((int i, SignalReference singleNodeSignal) in signal.Signal.ToSingleNodeSignals.Select(subcircuit.GetChildSignalReference).Index())
            AddSignalValue(singleNodeSignal, (value & 1<<i) > 0);
    }
}