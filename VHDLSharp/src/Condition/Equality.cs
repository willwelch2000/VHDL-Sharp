using VHDLSharp.LogicTree;
using VHDLSharp.Utility;

namespace VHDLSharp;

/// <summary>
/// Comparison between signal and either another signal or a value
/// </summary>
public class Equality : ConstantCondition
{
    /// <summary>
    /// Generate equality comparison between two signals
    /// </summary>
    /// <param name="mainSignal"></param>
    /// <param name="comparison">Signal to compare against</param>
    public Equality(NamedSignal mainSignal, ISignal comparison)
    {
        MainSignal = mainSignal;
        ComparisonSignal = comparison;
        CheckValid();
    }

    /// <summary>
    /// Main signal that gets evaluated
    /// </summary>
    public NamedSignal MainSignal { get; }

    /// <summary>
    /// If specified, the main signal is compared against this
    /// If null, the integer value is used instead
    /// </summary>
    public ISignal ComparisonSignal { get; }

    /// <inheritdoc/>
    public override IEnumerable<NamedSignal> InputSignals => ComparisonSignal is NamedSignal namedComparison ? [MainSignal, namedComparison] : [MainSignal];

    /// <inheritdoc/>
    public override string ToLogicString() => $"{MainSignal.Name} = {ComparisonSignal.ToLogicString()}";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <summary>
    /// Called after construction
    /// </summary>
    private void CheckValid()
    {
        if (!MainSignal.CanCombine(ComparisonSignal) || !ComparisonSignal.CanCombine(MainSignal))
            throw new Exception("Main signal is not compatible with comparison signal");
    }
}