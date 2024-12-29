
using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// A <see cref="EventDrivenCondition"/> that is true on a signal's falling edge
/// </summary>
public class FallingEdge(SingleNodeSignal signal) : EventDrivenCondition
{
    /// <summary>
    /// Signal used for the condition
    /// </summary>
    public SingleNodeSignal Signal { get; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => [Signal];

    /// <inheritdoc/>
    public override string ToLogicString() => $"falling_edge({Signal.Name})";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();
}