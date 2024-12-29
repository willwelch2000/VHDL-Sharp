using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using VHDLSharp.LogicTree;
using VHDLSharp.Utility;

namespace VHDLSharp;

/// <summary>
/// Behavior that uses sequential rather than combinational logic
/// Maps <see cref="Condition"/> objects to <see cref="CombinationalBehavior"/> objects
/// The output signal is assigned the value from the combinational behavior when the condition is met
/// Priority is used for the conditions
/// </summary>
public class DynamicBehavior : DigitalBehavior
{
    /// <summary>
    /// Ordered mapping of condition to behavior
    /// </summary>
    private ObservableCollection<(ILogicallyCombinable<Condition, ConditionLogicStringOptions> Condition, CombinationalBehavior Behavior)> ConditionMappings { get; } = [];

    /// <summary>
    /// Generate new dynamic behavior
    /// </summary>
    public DynamicBehavior()
    {
        ConditionMappings.CollectionChanged += CasesListUpdated;
    }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => ConditionMappings.SelectMany(c => c.Behavior.InputSignals.Union(c.Condition.BaseObjects.SelectMany(c => c.InputSignals))).Distinct();

    /// <inheritdoc/>
    public override Dimension Dimension => ConditionMappings.Any() ? ConditionMappings.First().Behavior.Dimension : new();

    /// <inheritdoc/>
    public override string ToVhdl(ISignal outputSignal)
    {
        if (ConditionMappings.Count == 0)
            throw new Exception("Must have at least one condition mapping");
        
        StringBuilder sb = new();
        sb.AppendLine($"process({string.Join(", ", InputSignals.Select(s => s.Name))}) is");
        sb.AppendLine("begin");

        // First condition
        (ILogicallyCombinable<Condition, ConditionLogicStringOptions> firstCondition, CombinationalBehavior firstBehavior) = ConditionMappings.First();
        sb.AppendLine($"\tif ({firstCondition.ToLogicString()}) then");
        sb.AppendLine(firstBehavior.ToVhdl(outputSignal).AddIndentation(2));

        // Remaining conditions
        foreach ((ILogicallyCombinable<Condition, ConditionLogicStringOptions> condition, CombinationalBehavior behavior) in ConditionMappings.Skip(1))
        {
            sb.AppendLine($"\telse if ({condition.ToLogicString()}) then");
            sb.AppendLine(behavior.ToVhdl(outputSignal).AddIndentation(2));
        }

        sb.AppendLine("\tend if;");
        sb.AppendLine("end process;");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public override void CheckValid()
    {
        base.CheckValid(); // Checks that everything is in just one module
        // Go through all after first and test compatibility with first
        if (ConditionMappings.Count > 1)
        {
            (ILogicallyCombinable<Condition, ConditionLogicStringOptions> condition, CombinationalBehavior behavior) first = ConditionMappings.First();
            foreach ((ILogicallyCombinable<Condition, ConditionLogicStringOptions> condition, CombinationalBehavior behavior) in ConditionMappings.Skip(1))
                if (!first.behavior.Dimension.Compatible(behavior.Dimension))
                    throw new Exception("Expressions are incompatible. Must have same dimension");
        }
    }

    private void CasesListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckValid();
    }
}