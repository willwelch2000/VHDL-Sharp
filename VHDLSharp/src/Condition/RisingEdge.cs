
namespace VHDLSharp;

/// <summary>
/// A <see cref="EventDrivenCondition"/> that is true on a signal's rising edge
/// </summary>
public class RisingEdge(SingleNodeSignal signal) : EventDrivenCondition
{
    /// <summary>
    /// Signal used for the condition
    /// </summary>
    public SingleNodeSignal Signal { get; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => [Signal];

    /// <inheritdoc/>
    public override string ToLogicString() => $"rising_edge({Signal.Name})";

    /// <inheritdoc/>
    public override string ToLogicString(ConditionLogicStringOptions options) => $"rising_edge({Signal.Name})";
}