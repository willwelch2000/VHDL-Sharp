using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Behaviors;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Validation;

namespace VHDLSharp.Conditions;

/// <summary>
/// Condition that compares 2 signals
/// </summary>
public class Comparison : ConstantCondition, IEquatable<Comparison>
{
    /// <summary>
    /// Constructor given two signals. Condition is true when the main signal is greater than the comparison
    /// </summary>
    /// <param name="mainSignal">Main signal</param>
    /// <param name="comparisonSignal">Signal the main signal is compared against</param>
    /// <param name="lessThan">
    /// If true, evaluates to true if the main signal is less than the comparison.
    /// If false, evaluates to true if the main signal is less than the comparison. 
    /// </param>
    /// <param name="signed">If true, two's complement notation is assumed for comparison</param>
    public Comparison(ISignal mainSignal, ISignal comparisonSignal, bool lessThan, bool signed=false)
    {
        MainSignal = mainSignal;
        ComparisonSignal = comparisonSignal;
        LessThan = lessThan;
        Signed = signed;
        ManageNewSignals([mainSignal, comparisonSignal]);
        // Check after construction
        if (!((IValidityManagedEntity)this).CheckTopLevelValidity(out Exception? exception))
            throw exception;
    }

    /// <summary>Main signal</summary>
    public ISignal MainSignal { get; }
    
    /// <summary>Signal that <see cref="MainSignal"/> is compared against</summary>
    public ISignal ComparisonSignal { get; }

    /// <summary>
    /// If true, evaluates to true if the main signal is less than the comparison.
    /// If false, evaluates to true if the main signal is less than the comparison.
    /// </summary>
    public bool LessThan { get; set; }

    /// <summary>If true, two's complement notation is assumed for comparison</summary>
    public bool Signed { get; set; }

    private IEnumerable<ISignal> InputSignals => [MainSignal, ComparisonSignal];

    /// <inheritdoc/>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => InputSignals.OfType<IModuleSpecificSignal>();

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context) =>
        !ValidityManager.IsValid(out Exception? issue) ? throw new InvalidException("Condition must be valid to evaluate", issue) :
        !((IValidityManagedEntity)context).ValidityManager.IsValid(out issue) ? throw new InvalidException("Subcircuit context must be valid to evluate condition", issue) :
        state.CurrentTimeStepIndex > 0 && EvaluateGivenValues(MainSignal.GetLastOutputValue(state, context), ComparisonSignal.GetLastOutputValue(state, context));

    private bool EvaluateGivenValues(int main, int comparison)
    {
        int dimension = MainSignal.Dimension.NonNullValue;
        if (Signed)
        {
            // Adjust for two's complement--MSB is negative not positive, so subtract 2 times that value
            int threshold = 1 << (dimension - 1);
            main = main >= threshold ? main - 2*threshold : main;
            comparison = comparison >= threshold ? comparison - 2*threshold : comparison;
        }
        return LessThan ? main < comparison : main > comparison;
    }

    /// <inheritdoc/>
    public override string ToLogicString()
    {
        if (!ValidityManager.IsValid(out Exception? issue))
            throw new InvalidException("Condition must be valid to get string", issue);

        string comparisonType = Signed ? "signed" : "unsigned";
        return $"{comparisonType}({MainSignal.ToLogicString()}) {(LessThan ? "<" : ">")} {comparisonType}({ComparisonSignal.ToLogicString()})";
    }
        
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
        if (!outputSignal.ParentModule.Equals(ParentModule) || ParentModule is null)
            throw new IncompatibleSignalException("Output signal must have same parent module as condition");

        int i = 0;
        int dimension = MainSignal.Dimension.NonNullValue;
        ILogicallyCombinable<ISignal> expression = null!;
        foreach ((ISingleNodeSignal mainSignalBit, ISingleNodeSignal compSignalBit) in MainSignal.ToSingleNodeSignals.Zip(ComparisonSignal.ToSingleNodeSignals))
        {
            // In signed comparison for bit one, it's true if main = 0 and comp = 1 (flipped for less-than)
            And<ISignal> comparison = (Signed && i == dimension - 1) != LessThan ? mainSignalBit.Not().And(compSignalBit) : mainSignalBit.And(compSignalBit.Not());
            if (i == 0)
                expression = comparison;
            else
            {
                // Expression becomes this bit comparison OR the built-up expression--more significant bits matter more
                ILogicallyCombinable<ISignal> equalBits = new Or<ISignal>(mainSignalBit.And(compSignalBit), mainSignalBit.Not().And(compSignalBit.Not()));
                expression = comparison.Or(new And<ISignal>(equalBits, expression));
            }
            i++;
        }

        return new LogicExpression(expression).GetSpice(outputSignal, uniqueId);
    }

    /// <inheritdoc/>
    public bool Equals(Comparison? other) => other is not null && 
        MainSignal.Equals(other.MainSignal) && ComparisonSignal.Equals(other.ComparisonSignal) &&
        LessThan.Equals(other.LessThan) && Signed.Equals(other.Signed);
        
    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Comparison);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode code = new();
        code.Add(MainSignal);
        code.Add(ComparisonSignal);
        code.Add(LessThan);
        code.Add(Signed);
        return code.ToHashCode();
    }
}