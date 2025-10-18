
using System.Diagnostics.CodeAnalysis;
using SpiceSharp.Components;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Conditions;

/// <summary>
/// An event-driven condition that is true on a signal's rising edge
/// </summary>
public class RisingEdge(ISingleNodeNamedSignal signal) : Condition, IEventDrivenCondition
{
    // No need to add child entities because signal is not a derived signal
    
    /// <summary>
    /// Signal used for the condition
    /// </summary>
    public ISingleNodeNamedSignal Signal { get; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> InputSignals => [Signal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context)
    {
        if (!((IValidityManagedEntity)context).ValidityManager.IsValid(out Exception? issue))
            throw new InvalidException("Subcircuit context must be valid to evluate condition", issue);
        SignalReference signalReference = context.GetChildSignalReference(Signal);
        bool[] values = [.. state.GetSingleNodeSignalValues(signalReference)];
        return values.Length > 1 && !values[^2] && values[^1];
    }

    /// <inheritdoc/>
    public override string ToLogicString() => $"rising_edge({Signal.Name})";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;
        return true;
    }

    /// <inheritdoc/>
    public SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal) =>
        !Signal.CanCombine(outputSignal) || !outputSignal.CanCombine(Signal) ? throw new IncompatibleSignalException("Output signal is not compatible with this condition") :
        new([new Resistor(SpiceUtil.GetSpiceName(uniqueId, 0, "connect"), Signal.Name, outputSignal.Name, 1e-3)]);
}