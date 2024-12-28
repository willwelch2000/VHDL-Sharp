namespace VHDLSharp;

/// <summary>
/// Abstract class defining a behavior that makes up a module
/// </summary>
public abstract class DigitalBehavior
{
    private EventHandler? behaviorUpdated;

    /// <summary>
    /// Get all of the input signals used in this behavior
    /// </summary>
    public abstract IEnumerable<ISignal> InputSignals { get; }

    /// <summary>
    /// Get VHDL representation given the assigned output signal
    /// </summary>
    public abstract string ToVhdl(ISignal outputSignal);

    /// <summary>
    /// Dimension of behavior, or null if it has no set dimension
    /// </summary>
    public abstract Dimension Dimension { get; }

    /// <summary>
    /// Event called when a property of the behavior is changed that could affect other objects
    /// </summary>
    public event EventHandler? BehaviorUpdated
    {
        add
        {
            behaviorUpdated -= value; // remove if already present
            behaviorUpdated += value;
        }
        remove => behaviorUpdated -= value;
    }

    /// <summary>
    /// Module this behavior refers to, found from the signals
    /// Null if no input signals, meaning that it has no specific module
    /// </summary>
    public Module? Module
    {
        get
        {
            CheckValid();
            return InputSignals.FirstOrDefault()?.Parent;
        }
    }

    /// <summary>
    /// Checks that the behavior is valid given the input signals
    /// Base version just checks that all input signals come from the same module
    /// </summary>
    /// <exception cref="Exception"></exception>
    public virtual void CheckValid()
    {
        var modules = InputSignals.Select(s => s.Parent).Distinct();
        if (modules.Count() != 1)
            throw new Exception("Input signals should all come from the same module");
    }

    /// <summary>
    /// Call this method to raise the <see cref="BehaviorUpdated"/> event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void RaiseBehaviorChanged(object? sender, EventArgs e)
    {
        behaviorUpdated?.Invoke(sender, e);
    }
}