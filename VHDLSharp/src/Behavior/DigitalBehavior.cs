namespace VHDLSharp;

/// <summary>
/// Interface defining a behavior that makes up a module
/// </summary>
public abstract class DigitalBehavior
{
    /// <summary>
    /// Get all of the input signals used in this behavior
    /// </summary>
    public abstract IEnumerable<ISignal> InputSignals { get; }

    /// <summary>
    /// Get the output signal for this behavior
    /// </summary>
    public abstract ISignal OutputSignal { get; }

    /// <summary>
    /// Event called when a property of the behavior is changed that could affect other objects
    /// </summary>
    public event EventHandler? BehaviorUpdated;

    /// <summary>
    /// Module this behavior refers to, found from the signals
    /// </summary>
    public Module Module
    {
        get
        {
            CheckValid();
            return OutputSignal.Parent;
        }
    }

    /// <summary>
    /// Checks that the behavior is valid given the input and output signals
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void CheckValid()
    {
        var modules = InputSignals.Select(s => s.Parent).Append(OutputSignal.Parent).Distinct();
        if (modules.Count() != 1)
            throw new Exception("Input and output signals should come from the same module");
    }
}