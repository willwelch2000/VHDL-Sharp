namespace VHDLSharp;

/// <summary>
/// Behavior where an output signal is set based on a selector signal's value
/// </summary>
public class CaseBehavior : DigitalBehavior
{
    private readonly LogicExpression?[] cases;

    private readonly Module module;

    private readonly SingleNodeSignal output;

    private LogicExpression? defaultExpression;

    /// <summary>
    /// Generate case behavior given selector and output signals
    /// </summary>
    /// <param name="selector"></param>
    /// <param name="output"></param>
    /// <exception cref="Exception"></exception>
    public CaseBehavior(ISignal selector, SingleNodeSignal output)
    {
        Selector = selector;
        this.output = output;
        cases = new LogicExpression[1 << selector.Dimension];
        module = selector.Parent;

        if (output.Parent != module)
            throw new Exception("Output signal must be in same module as selector");
    }

    /// <summary>
    /// Selector signal
    /// </summary>
    public ISignal Selector { get; private init; }

    /// <summary>
    /// Expression for default case
    /// </summary>
    public LogicExpression? DefaultExpression
    {
        get => defaultExpression;
        set
        {
            if (value is not null && value.Parent != module)
                throw new Exception("The logic expression must be in the same module.");
            defaultExpression = value;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => cases.Where(c => c is not null).SelectMany(c => c?.Signals ?? []).Append(Selector);

    /// <inheritdoc/>
    public override SingleNodeSignal OutputSignal => output;

    /// <summary>
    /// Indexer for the logic expression of each case
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LogicExpression? this[int index]
    {
        get
        {
            if (index < 0 || index >= cases.Length)
                throw new Exception($"Case value must be between 0 and {cases.Length-1}");
            return cases[index];
        }
        set => AddCase(index, value);
    }

    /// <summary>
    /// Add a logic expression for a case
    /// </summary>
    /// <param name="value"></param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public void AddCase(int value, LogicExpression? logicExpression)
    {
        if (value < 0 || value >= cases.Length)
            throw new Exception($"Case value must be between 0 and {cases.Length-1}");
        if (logicExpression is not null && logicExpression.Parent != module)
            throw new Exception("The logic expression must be in the same module.");

        cases[value] = logicExpression;
        RaiseBehaviorChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Is the behavior ready to be used
    /// </summary>
    /// <returns></returns>
    public bool Complete() => cases.All(c => c is not null) || defaultExpression is not null;
}