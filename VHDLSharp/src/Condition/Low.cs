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
/// Condition that is true if this signal is low
/// </summary>
public class Low : ConstantCondition, IEquatable<Low>
{
    /// <summary>
    /// Constructor given trigger signal
    /// </summary>
    /// <param name="signal">Signal that is tested high or low</param>
    public Low(ISingleNodeModuleSpecificSignal signal)
    {
        Signal = signal;
        ManageNewSignals([signal]);
    }
    
    /// <summary>Signal that gets evaluated</summary>
    public ISingleNodeModuleSpecificSignal Signal { get; }

    /// <inheritdoc/>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => [Signal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context) =>
        !((IValidityManagedEntity)context).ValidityManager.IsValid(out Exception? issue) ? throw new InvalidException("Subcircuit context must be valid to evluate condition", issue) :
        state.CurrentTimeStepIndex > 0 &&
        Signal.GetLastOutputValue(state, context) == 0;

    /// <inheritdoc/>
    public override string ToLogicString() => $"{Signal.ToLogicString()} is low";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal)
    {
        if (!outputSignal.ParentModule.Equals(ParentModule))
            throw new IncompatibleSignalException("Output signal must have same parent module as condition");

        // Just invert the signal
        List<IEntity> entities = [];
        INamedSubcircuitDefinition not = SpiceUtil.GetNotSubcircuit();
        entities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, 0, "NOT1"), not, Signal.GetSpiceName(), outputSignal.GetSpiceName()));

        return new SpiceCircuit(entities).WithCommonEntities();
    }
    

    /// <inheritdoc/>
    public bool Equals(Low? other) => other is not null && Signal.Equals(other.Signal);
        
    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Low);

    /// <inheritdoc/>
    public override int GetHashCode() => Signal.GetHashCode();
}