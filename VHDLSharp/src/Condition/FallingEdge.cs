
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharp.Conditions;

/// <summary>
/// An event-driven condition that is true on a signal's falling edge
/// </summary>
public class FallingEdge(ISingleNodeNamedSignal signal) : Condition, IEventDrivenCondition
{
    /// <summary>
    /// Signal used for the condition
    /// </summary>
    public ISingleNodeNamedSignal Signal { get; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> InputSignals => [Signal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context)
    {
        SignalReference signalReference = context.GetChildSignalReference(Signal);
        bool[] values = [.. state.GetSingleNodeSignalValues(signalReference)];
        return values.Length > 1 && values[^2] && !values[^1];
    }

    /// <inheritdoc/>
    public override string ToLogicString() => $"falling_edge({Signal.Name})";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;
        return true;
    }
}