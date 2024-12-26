namespace VHDLSharp;

public class DynamicBehavior : DigitalBehavior
{
    private readonly List<(Condition condition, CombinationalBehavior behavior)> cases = [];

    private readonly ISignal outputSignal;

    public DynamicBehavior(ISignal outputSignal)
    {

    }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => cases.SelectMany(c => c.behavior.InputSignals).Distinct();

    /// <inheritdoc/>
    public override ISignal OutputSignal => outputSignal;

    public override string ToVhdl => throw new NotImplementedException();
}