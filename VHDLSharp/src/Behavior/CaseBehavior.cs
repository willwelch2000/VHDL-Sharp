using System.Text;

namespace VHDLSharp;

/// <summary>
/// Behavior where an output signal is set based on a selector signal's value
/// </summary>
public class CaseBehavior : DigitalBehavior
{
    private readonly LogicExpression?[] cases;

    private readonly Module module;

    private readonly ISignal outputSignal;

    private LogicExpression? defaultExpression;

    /// <summary>
    /// Generate case behavior given selector and output signals
    /// </summary>
    /// <param name="selector"></param>
    /// <param name="outputSignal"></param>
    /// <exception cref="Exception"></exception>
    public CaseBehavior(ISignal selector, ISignal outputSignal)
    {
        Selector = selector;
        this.outputSignal = outputSignal;
        cases = new LogicExpression[1 << selector.Dimension];
        module = selector.Parent;

        if (outputSignal.Parent != module)
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
            if (value is not null)
            {
                if (value.Parent != module)
                    throw new Exception("The logic expression must be in the same module.");
                if (value.Dimension != outputSignal.Dimension)
                    throw new Exception("This logic expression must have the same dimension as the output signal.");
            }
            defaultExpression = value;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => cases.Where(c => c is not null).SelectMany(c => c?.Signals ?? []).Append(Selector).Distinct();

    /// <inheritdoc/>
    public override ISignal OutputSignal => outputSignal;

    /// <inheritdoc/>
    public override string ToVhdl
    {
        get
        {
            if (!Complete())
                throw new Exception("Case behavior must be complete to convert to VHDL");

            StringBuilder sb = new();
            sb.AppendLine($"process({string.Join(", ", InputSignals.Select(s => s.Name))}) is");
            sb.AppendLine("begin");
            sb.AppendLine($"\tcase {Selector.Name} is");

            // Cases
            for (int i = 0; i < cases.Length; i++)
            {
                LogicExpression? expression = cases[i];
                if (expression is null)
                    continue;
                sb.AppendLine($"\t\twhen \"{i.ToBinaryString(Selector.Dimension)}\" =>");
                sb.AppendLine($"\t\t\t{outputSignal} <= {expression.ToVhdl};");
            }

            // Default
            if (defaultExpression is not null)
            {
                sb.AppendLine($"\t\twhen others =>");
                sb.AppendLine($"\t\t\t{outputSignal} <= {defaultExpression.ToVhdl};");
            }

            sb.AppendLine("\tend case;");
            sb.AppendLine("end process;");

            return sb.ToString();
        }
    }

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
                throw new ArgumentException($"Case value must be between 0 and {cases.Length-1}");
            return cases[index];
        }
        set => AddCase(index, value);
    }

    /// <summary>
    /// Indexer for the logic expressions given a boolean case
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LogicExpression? this[bool index]
    {
        get
        {
            if (Selector.Dimension != 1)
                throw new Exception("Selector dimension must be 1 for boolean value");
            return this[index ? 1 : 0];
        }
        set
        {
            if (Selector.Dimension != 1)
                throw new Exception("Selector dimension must be 1 for boolean value");
            this[index ? 1 : 0] = value;
        }
    }

    /// <summary>
    /// Add a logic expression for a case
    /// </summary>
    /// <param name="value">integer value for selector</param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public void AddCase(int value, LogicExpression? logicExpression)
    {
        if (value < 0 || value >= cases.Length)
            throw new Exception($"Case value must be between 0 and {cases.Length-1}");
        if (logicExpression is not null)
        {
            if (logicExpression.Parent != module)
                throw new Exception("The logic expression must be in the same module.");
            if (logicExpression.Dimension != outputSignal.Dimension)
                throw new Exception("This logic expression must have the same dimension as the output signal.");
        }

        cases[value] = logicExpression;
        RaiseBehaviorChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Add a logic expression for a case
    /// Selector must have dimension of 1
    /// </summary>
    /// <param name="value">boolean value for selector</param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public void AddCase(bool value, LogicExpression? logicExpression)
    {
        if (Selector.Dimension != 1)
            throw new Exception("Selector dimension must be 1 for boolean value");
        AddCase(value ? 1 : 0, logicExpression);
    }

    /// <summary>
    /// Is the behavior ready to be used
    /// </summary>
    /// <returns></returns>
    public bool Complete() => cases.All(c => c is not null) || defaultExpression is not null;
}