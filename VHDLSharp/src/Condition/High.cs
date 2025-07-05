using System.Diagnostics.CodeAnalysis;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Conditions;

/// <summary>
/// Condition that is true if this signal is high
/// </summary>
/// <param name="signal">Input signal to test</param>
public class High(ISingleNodeNamedSignal signal) : Condition, IConstantCondition
{
    /// <summary>Signal that gets evaluated</summary>
    public ISingleNodeNamedSignal Signal { get; } = signal;

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> InputSignals => [Signal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context) =>
        !((IValidityManagedEntity)context).ValidityManager.IsValid() ? throw new InvalidException("Subcircuit context must be valid to evluate condition") :
        state.CurrentTimeStepIndex > 0 &&
        Signal.GetLastOutputValue(state, context) == 1;

    /// <inheritdoc/>
    public override string ToLogicString() => $"{Signal.Name} is high";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal)
    {
        if (!outputSignal.ParentModule.Equals(ParentModule))
            throw new IncompatibleSignalException("Output signal must have same parent module as condition");

        // Just buffer the signal
        List<IEntity> entities = [];
        INamedSubcircuitDefinition not = SpiceUtil.GetNotSubcircuit();
        string intNode = SpiceUtil.GetSpiceName(uniqueId, 0, "NotNode");
        entities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, 0, "NOT1"), not, Signal.GetSpiceName(), intNode));
        entities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, 0, "NOT2"), not, intNode, outputSignal.GetSpiceName()));

        return new SpiceCircuit(entities).WithCommonEntities();
    }
}