
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;

namespace VHDLSharp.Conditions;

/// <summary>
/// A <see cref="EventDrivenCondition"/> that is true on a signal's falling edge
/// </summary>
public class FallingEdge(ISingleNodeNamedSignal signal) : EventDrivenCondition
{
    /// <summary>
    /// Signal used for the condition
    /// </summary>
    public ISingleNodeNamedSignal Signal { get; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> InputSignals => [Signal];

    /// <inheritdoc/>
    public override string ToLogicString() => $"falling_edge({Signal.Name})";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();
}