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
    public Equality(ISignal mainSignal, ISignal comparison)
    {
        MainSignal = mainSignal;
        ComparisonSignal = comparison;
        CheckValid();
    }

    /// <summary>
    /// Generate equality comparison between signal and integer value
    /// </summary>
    /// <param name="mainSignal"></param>
    /// <param name="comparison">Integer value to compare against</param>
    public Equality(ISignal mainSignal, int comparison)
    {
        MainSignal = mainSignal;
        ComparisonValue = comparison;
        CheckValid();
    }

    /// <summary>
    /// Main signal that gets evaluated
    /// </summary>
    public ISignal MainSignal { get; }

    /// <summary>
    /// If specified, the main signal is compared against this
    /// If null, the integer value is used instead
    /// </summary>
    public ISignal? ComparisonSignal { get; }

    /// <summary>
    /// If <see cref="ComparisonSignal"/> is null, this is used for comparison
    /// </summary>
    public int? ComparisonValue { get; }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => ComparisonSignal is null ? [MainSignal] : [MainSignal, ComparisonSignal];

    /// <inheritdoc/>
    public override string ToLogicString()
    {
        if (ComparisonSignal is not null)
        {
            return $"{MainSignal.Name} = {ComparisonSignal.Name}";
        }
        else
        {
            return $"{MainSignal.Name} = \"{ComparisonValue?.ToBinaryString(MainSignal.Dimension.NonNullValue) ?? throw new("Should be impossible")}\"";
        }
    }

    /// <summary>
    /// Called after construction
    /// </summary>
    private void CheckValid()
    {
        Dimension dimension = MainSignal.Dimension;
        if (ComparisonSignal is null)
        {
            if (dimension.Value is not null && ComparisonValue < 0 || ComparisonValue >= 1<<dimension.Value)
                throw new Exception($"Comparison value must be between 0 and {(1<<dimension.Value)-1}");
        }
        else if (!MainSignal.CanCombine(ComparisonSignal))
            throw new Exception("Main signal is not compatible with comparison signal");
    }
}