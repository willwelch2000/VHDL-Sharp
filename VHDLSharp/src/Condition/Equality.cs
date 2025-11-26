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

// TODO make main signal ISignal
/// <summary>
/// Comparison between signal and either another signal or a value
/// </summary>
public class Equality : ConstantCondition, IEquatable<Equality>
{
    /// <summary>
    /// Generate equality comparison between two signals
    /// </summary>
    /// <param name="mainSignal"></param>
    /// <param name="comparison">Signal to compare against</param>
    public Equality(IModuleSpecificSignal mainSignal, ISignal comparison)
    {
        MainSignal = mainSignal;
        ComparisonSignal = comparison;
        // Add comparison if it's a derived signal
        ManageNewSignals([mainSignal, comparison]);
        // Check after construction
        if (!((IValidityManagedEntity)this).CheckTopLevelValidity(out Exception? exception))
            throw exception;
    }

    /// <summary>
    /// Main signal that gets evaluated
    /// </summary>
    public IModuleSpecificSignal MainSignal { get; }

    /// <summary>
    /// The main signal is compared against this
    /// </summary>
    public ISignal ComparisonSignal { get; }

    /// <inheritdoc/>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => ComparisonSignal is IModuleSpecificSignal namedComparison ? [MainSignal, namedComparison] : [MainSignal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context) =>
        !ValidityManager.IsValid(out Exception? issue) ? throw new InvalidException("Condition must be valid to evaluate", issue) :
        !((IValidityManagedEntity)context).ValidityManager.IsValid(out issue) ? throw new InvalidException("Subcircuit context must be valid to evluate condition", issue) :
        state.CurrentTimeStepIndex > 0 &&
        MainSignal.GetLastOutputValue(state, context) == ComparisonSignal.GetLastOutputValue(state, context);

    /// <inheritdoc/>
    public override string ToLogicString() =>
        !ValidityManager.IsValid(out Exception? issue) ? throw new InvalidException("Condition must be valid to get string", issue) :
        $"{MainSignal.ToLogicString()} = {ComparisonSignal.ToLogicString()}";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        if (!MainSignal.CanCombine(ComparisonSignal) || !ComparisonSignal.CanCombine(MainSignal))
        {
            exception = new IncompatibleSignalException("Main signal is not compatible with comparison signal");
            return false;
        }
        exception = null;
        return true;
    }

    /// <inheritdoc/>
    public override SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal)
    {
        if (!ValidityManager.IsValid(out Exception? issue))
            throw new InvalidException("Condition must be valid to get Spice representation", issue);
        if (!outputSignal.ParentModule.Equals(ParentModule))
            throw new IncompatibleSignalException("Output signal must have same parent module as condition");

        List<IEntity> entities = [];
        Signal[] intermediateSignals = [.. Enumerable.Range(0, MainSignal.Dimension.NonNullValue).Select(i => new Signal(SpiceUtil.GetSpiceName(uniqueId, i, "equalityInt"), MainSignal.ParentModule))];
        string[] mainNames = [.. MainSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
        string[] compNames = [.. ComparisonSignal.ToSingleNodeSignals.Select(s => s.GetSpiceName())];

        // Single-dimension case--simpler
        if (MainSignal.Dimension.NonNullValue == 1)
            entities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, 0, "equalityXnor"), SpiceUtil.GetXnorSubcircuit(2), mainNames[0], compNames[0], outputSignal.GetSpiceName()));
        else
        {
            // XNOR for each pair of single-node signals
            for (int i = 0; i < MainSignal.Dimension.NonNullValue; i++)
                entities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, i, "equalityXnorInt"), SpiceUtil.GetXnorSubcircuit(2), mainNames[i], compNames[i], intermediateSignals[i].GetSpiceName()));
            // AND for the intermediate signals
            entities.Add(new Subcircuit(SpiceUtil.GetSpiceName(uniqueId, 0, "equalityAndFinal"), SpiceUtil.GetAndSubcircuit(intermediateSignals.Length), [.. intermediateSignals.Select(s => s.GetSpiceName()), outputSignal.GetSpiceName()]));
        }

        return new SpiceCircuit(entities).WithCommonEntities();
    }
    
    /// <inheritdoc/>
    public bool Equals(Equality? other) => other is not null && 
        MainSignal.Equals(other.MainSignal) && ComparisonSignal.Equals(other.ComparisonSignal);
        
    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Equality);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode code = new();
        code.Add(MainSignal);
        code.Add(ComparisonSignal);
        return code.ToHashCode();
    }
}