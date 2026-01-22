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
public class High : ConstantCondition, IEquatable<High>
{
    /// <summary>
    /// Constructor given trigger signal
    /// </summary>
    /// <param name="signal">Signal that is tested high or low</param>
    public High(ISingleNodeModuleSpecificSignal signal)
    {
        Signal = signal;
        ManageNewSignals([signal]);
    }
    
    /// <summary>Signal that gets evaluated</summary>
    public ISingleNodeModuleSpecificSignal Signal { get; }

    /// <inheritdoc/>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => [Signal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubmoduleReference context) =>
        !((IValidityManagedEntity)context).ValidityManager.IsValid(out Exception? issue) ? throw new InvalidException("Submodule context must be valid to evluate condition", issue) :
        state.CurrentTimeStepIndex > 0 &&
        Signal.GetLastOutputValue(state, context) == 1;

    /// <inheritdoc/>
    public override string ToLogicString() => $"{Signal.ToLogicString()} is high";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal)
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

    /// <inheritdoc/>
    public bool Equals(High? other) => other is not null && Signal.Equals(other.Signal);
        
    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as High);

    /// <inheritdoc/>
    public override int GetHashCode() => Signal.GetHashCode();
}