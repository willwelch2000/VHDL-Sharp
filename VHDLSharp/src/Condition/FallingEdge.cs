
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
/// An event-driven condition that is true on a signal's falling edge
/// </summary>
public class FallingEdge : Condition, IEventDrivenCondition, IEquatable<FallingEdge>
{
    /// <summary>
    /// Constructor given trigger signal
    /// </summary>
    /// <param name="signal">Signal to trigger on</param>
    public FallingEdge(ISingleNodeModuleSpecificSignal signal)
    {
        Signal = signal;
        ManageNewSignals([signal]);
    }

    /// <summary>
    /// Signal used for the condition
    /// </summary>
    public ISingleNodeModuleSpecificSignal Signal { get; }

    /// <inheritdoc/>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => [Signal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context)
    {
        if (!((IValidityManagedEntity)context).ValidityManager.IsValid(out Exception? issue))
            throw new InvalidException("Subcircuit context must be valid to evluate condition", issue);
        SignalReference signalReference = context.GetChildSignalReference(Signal.AsNamedSignal());
        List<bool> values = state.GetSingleNodeSignalValuesWithoutNewList(signalReference);
        return values.Count > 1 && values[^2] && !values[^1];
    }

    /// <inheritdoc/>
    public override string ToLogicString() => $"falling_edge({Signal.ToLogicString()})";

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
        new SpiceCircuit([new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, 0, "not"), SpiceUtil.GetNotSubcircuit(), Signal.GetSpiceName(), outputSignal.Name)]).WithCommonEntities();
        
    /// <inheritdoc/>
    public bool Equals(FallingEdge? other) => other is not null && Signal.Equals(other.Signal);
        
    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as FallingEdge);

    /// <inheritdoc/>
    public override int GetHashCode() => Signal.GetHashCode();
}