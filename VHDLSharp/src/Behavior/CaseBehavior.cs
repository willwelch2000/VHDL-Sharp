using System.Text;
using VHDLSharp.LogicTree;
using VHDLSharp.Utility;

namespace VHDLSharp;

/// <summary>
/// A behavior where an output signal is set based on a selector signal's value
/// </summary>
/// <param name="selector"></param>
public class CaseBehavior(ISignal selector) : CombinationalBehavior
{
    private readonly ILogicallyCombinable<ISignal>?[] caseExpressions = new ILogicallyCombinable<ISignal>[1 << selector.Dimension];

    private ILogicallyCombinable<ISignal>? defaultExpression;

    /// <summary>
    /// Selector signal
    /// </summary>
    public ISignal Selector { get; } = selector;

    /// <summary>
    /// Expression for default case
    /// </summary>
    public ILogicallyCombinable<ISignal>? DefaultExpression
    {
        get => defaultExpression;
        set
        {
            CheckCompatible(value);
            defaultExpression = value;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => caseExpressions.Where(c => c is not null).SelectMany(c => c?.BaseObjects ?? []).Append(Selector).Distinct();

    /// <inheritdoc/>
    public override int? Dimension
    {
        get
        {
            CheckValid();
            ILogicallyCombinable<ISignal>? nonNullCase = caseExpressions.Where(c => c is not null).FirstOrDefault();
            if (nonNullCase is null)
                return null;
            return nonNullCase.GetDimension();
        }
    }

    /// <inheritdoc/>
    public override string ToVhdl(ISignal outputSignal)
    {
        if (!Complete())
            throw new Exception("Case behavior must be complete to convert to VHDL");

        StringBuilder sb = new();
        sb.AppendLine($"process({string.Join(", ", InputSignals.Select(s => s.Name))}) is");
        sb.AppendLine("begin");
        sb.AppendLine($"\tcase {Selector.Name} is");

        // Cases
        for (int i = 0; i < caseExpressions.Length; i++)
        {
            ILogicallyCombinable<ISignal>? expression = caseExpressions[i];
            if (expression is null)
                continue;
            sb.AppendLine($"\t\twhen \"{i.ToBinaryString(Selector.Dimension)}\" =>");
            sb.AppendLine($"\t\t\t{outputSignal} <= {expression.ToLogicString()};");
        }

        // Default
        if (defaultExpression is not null)
        {
            sb.AppendLine($"\t\twhen others =>");
            sb.AppendLine($"\t\t\t{outputSignal} <= {defaultExpression.ToLogicString()};");
        }

        sb.AppendLine("\tend case;");
        sb.AppendLine("end process;");

        return sb.ToString();
    }

    /// <summary>
    /// Indexer for the logic expression of each case
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public ILogicallyCombinable<ISignal>? this[int index]
    {
        get
        {
            if (index < 0 || index >= caseExpressions.Length)
                throw new ArgumentException($"Case value must be between 0 and {caseExpressions.Length-1}");
            return caseExpressions[index];
        }
        set => AddCase(index, value);
    }

    /// <summary>
    /// Indexer for the logic expressions given a boolean case
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public ILogicallyCombinable<ISignal>? this[bool index]
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
    
    /// <inheritdoc/>
    public override void CheckValid()
    {
        base.CheckValid();
        // Get all non-null expressions
        List<ILogicallyCombinable<ISignal>> nonNullCases = [];
        foreach (ILogicallyCombinable<ISignal>? expression in caseExpressions.Append(DefaultExpression))
            if (expression is not null)
                nonNullCases.Add(expression);
        // Go through all after first and test compatibility with first
        if (nonNullCases.Count > 1)
            foreach (ILogicallyCombinable<ISignal> expression in nonNullCases.Skip(1))
                // CanCombine function can be used to test compatibility
                if (!nonNullCases[0].CanCombine(expression))
                    throw new Exception("Expressions are incompatible");
    }

    /// <summary>
    /// Add a logic expression for a case
    /// </summary>
    /// <param name="value">integer value for selector</param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public void AddCase(int value, ILogicallyCombinable<ISignal>? logicExpression)
    {
        if (value < 0 || value >= caseExpressions.Length)
            throw new Exception($"Case value must be between 0 and {caseExpressions.Length-1}");
        CheckCompatible(logicExpression);
        caseExpressions[value] = logicExpression;
        RaiseBehaviorChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Add a logic expression for a case
    /// Selector must have dimension of 1
    /// </summary>
    /// <param name="value">boolean value for selector</param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public void AddCase(bool value, ILogicallyCombinable<ISignal>? logicExpression)
    {
        if (Selector.Dimension != 1)
            throw new Exception("Selector dimension must be 1 for boolean value");
        AddCase(value ? 1 : 0, logicExpression);
    }

    /// <summary>
    /// Is the behavior ready to be used
    /// </summary>
    /// <returns></returns>
    public bool Complete() => caseExpressions.All(c => c is not null) || defaultExpression is not null;

    /// <summary>
    /// Checks if new (nullable) logic expression is compatible with the current state, given the expressions that have already been assigned
    /// </summary>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    private void CheckCompatible(ILogicallyCombinable<ISignal>? logicExpression)
    {
        // Fine if new one is null
        if (logicExpression is null)
            return;
        ILogicallyCombinable<ISignal>? nonNullCase = caseExpressions.Append(DefaultExpression).Where(c => c is not null).FirstOrDefault();
        // Fine if none have yet been assigned
        if (nonNullCase is null)
            return;
        // The CanCombine function can be used to see if this expression is compatible with a pre-existing expression
        if (!logicExpression.CanCombine(nonNullCase))
            throw new Exception($"Given expression is incompatible with pre-existing expression (must have parent {Module} and dimension must be {Dimension?.ToString() ?? "N/A"})");
    }
}