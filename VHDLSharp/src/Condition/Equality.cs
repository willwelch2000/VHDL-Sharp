using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;

namespace VHDLSharp.Conditions;

/// <summary>
/// Comparison between signal and either another signal or a value
/// </summary>
public class Equality : Condition, IConstantCondition
{
    /// <summary>
    /// Generate equality comparison between two signals
    /// </summary>
    /// <param name="mainSignal"></param>
    /// <param name="comparison">Signal to compare against</param>
    public Equality(INamedSignal mainSignal, ISignal comparison)
    {
        MainSignal = mainSignal;
        ComparisonSignal = comparison;
        // Check after construction
        if (!((IValidityManagedEntity)this).CheckTopLevelValidity(out Exception? exception))
            throw exception;
    }

    /// <summary>
    /// Main signal that gets evaluated
    /// </summary>
    public INamedSignal MainSignal { get; }

    /// <summary>
    /// If specified, the main signal is compared against this
    /// If null, the integer value is used instead
    /// </summary>
    public ISignal ComparisonSignal { get; }

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> InputSignals => ComparisonSignal is INamedSignal namedComparison ? [MainSignal, namedComparison] : [MainSignal];

    /// <inheritdoc/>
    public override bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context) => 
        state.CurrentTimeStepIndex > 0 &&
        MainSignal.GetLastOutputValue(state, context) == ComparisonSignal.GetLastOutputValue(state, context);

    /// <inheritdoc/>
    public override string ToLogicString() => $"{MainSignal.Name} = {ComparisonSignal.ToLogicString()}";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        if (!MainSignal.CanCombine(ComparisonSignal) || !ComparisonSignal.CanCombine(MainSignal))
        {
            exception =  new IncompatibleSignalException("Main signal is not compatible with comparison signal");
            return false;
        }
        exception = null;
        return true;
    }
}