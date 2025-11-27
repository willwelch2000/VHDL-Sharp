using VHDLSharp.Signals;

namespace VHDLSharp.Simulations;

/// <summary>
/// Contains all info regarding the current state of a <see cref="RuleBasedSimulation"/>
/// </summary>
public class RuleBasedSimulationState
{
    /// <summary>
    /// Map of single-node (and fully ascended!) signal references to their boolean values for the timesteps.
    /// This is the ultimate source of truth
    /// </summary>
    private Dictionary<SignalReference, List<bool>> singleNodeSignalValues = [];

    private List<double> allTimeSteps = [0];

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
    /// Current index of time step in the list
    /// </summary>
    public int CurrentTimeStepIndex => allTimeSteps.Count - 1;

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
    public List<bool> GetSingleNodeSignalValues(SignalReference signal) => [.. GetSingleNodeSignalValuesWithoutNewList(signal)];

    // Version that can be used internally and doesn't make a new list
    private List<bool> GetSingleNodeSignalValuesWithoutNewList(SignalReference signal)
    {
        // Ascend to higher module, if necessary
        SignalReference ascended = signal.Ascend();
        return ascended.Signal switch
        {
            ISingleNodeNamedSignal => singleNodeSignalValues.TryGetValue(ascended, out List<bool>? vals) ? vals :
                CurrentTimeStepIndex == 0 ? singleNodeSignalValues[ascended] = [] : throw new Exception("New signals can't be added after first timestep complete"),
            _ => throw new Exception("Signal reference must be to a single-node signal")
        };
    }

    internal bool GetLastSingleNodeSignalValue(SignalReference signal, int? lastIndex = null)
    {
        try
        {
            return GetSingleNodeSignalValuesWithoutNewList(signal)[lastIndex ?? CurrentTimeStepIndex - 1];
        }
        catch (IndexOutOfRangeException)
        {
            throw new Exception("Signal does not have a value from the previous time step");
        }
    }

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
                return [.. GetSingleNodeSignalValuesWithoutNewList(signal).Select(val => val ? 1 : 0)];

            SubcircuitReference subcircuit = signal.Subcircuit;
            SignalReference[] singleNodeSignals = [.. signal.Signal.ToSingleNodeSignals.Select(subcircuit.GetChildSignalReference)];
            List<int> values = [];
            // Go through length of first result
            for (int i = 0; i < GetSingleNodeSignalValuesWithoutNewList(singleNodeSignals[0]).Count; i++)
                values.Add(singleNodeSignals.Select((s, j) => GetSingleNodeSignalValuesWithoutNewList(s)[i] ? 1 << j : 0).Sum());

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

    internal int GetLastSignalValue(SignalReference signal, int? lastIndex = null)
    {
        try
        {
            SubcircuitReference subcircuit = signal.Subcircuit;
            SignalReference[] singleNodeSignals = [.. signal.Signal.ToSingleNodeSignals.Select(subcircuit.GetChildSignalReference)];
            return singleNodeSignals.Select((s, j) => GetLastSingleNodeSignalValue(s, lastIndex) ? 1 << j : 0).Sum();
        }
        catch (KeyNotFoundException keyEx)
        {
            throw new Exception($"Signal not added to simulation state", keyEx);
        }
    }

    /// <summary>
    /// Get all single-node signals that have been assigned values
    /// </summary>
    public IEnumerable<SignalReference> AllSingleNodeSignals => [.. singleNodeSignalValues.Keys];

    /// <summary>
    /// Add a single-node signal value as a boolean
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="value"></param>
    internal void AddSignalValue(SignalReference signal, bool value)
    {
        if (signal.Signal is not ISingleNodeNamedSignal)
            throw new Exception("Must be a single-node signal to add a bool value");

        // Ascend to higher module, if necessary
        SignalReference ascended = signal.Ascend();
        if (singleNodeSignalValues.TryGetValue(ascended, out List<bool>? values))
            values.Add(value);
        else if (CurrentTimeStepIndex == 0)
            singleNodeSignalValues[ascended] = [value];
        else
            throw new Exception("New signals can't be added after first timestep complete");
    }

    /// <summary>
    /// Add a signal value
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="value"></param>
    internal void AddSignalValue(SignalReference signal, int value)
    {
        foreach ((int i, SignalReference singleNodeSignal) in signal.GetSingleNodeReferences().Index())
            AddSignalValue(singleNodeSignal, (value & 1<<i) > 0);
    }

    /// <summary>
    /// Get a state initialized given values and timesteps
    /// </summary>
    /// <param name="signalValues"></param>
    /// <param name="allTimeSteps"></param>
    /// <param name="currentTimeStep"></param>
    /// <returns></returns>
    public static RuleBasedSimulationState GivenStartingPoint(Dictionary<SignalReference, List<int>> signalValues, List<double> allTimeSteps, double currentTimeStep)
    {
        Dictionary<SignalReference, List<bool>> singleNodeSignalValues = [];
        foreach ((SignalReference signal, List<int> values) in signalValues)
            foreach ((int i, SignalReference singleNodeRef) in signal.GetSingleNodeReferences().Index())
                singleNodeSignalValues[singleNodeRef] = [.. values.Select(v => (v & 1<<i) > 0)];

        RuleBasedSimulationState state = new()
        {
            singleNodeSignalValues = singleNodeSignalValues,
            allTimeSteps = allTimeSteps,
            currentTimeStep = currentTimeStep,
        };
        return state;
    }
}