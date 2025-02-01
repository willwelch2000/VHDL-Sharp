using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using VHDLSharp.LogicTree;
using VHDLSharp.Utility;
using VHDLSharp.Conditions;
using VHDLSharp.Signals;
using VHDLSharp.Dimensions;
using SpiceSharp.Entities;

namespace VHDLSharp.Behaviors;

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
    private ObservableCollection<(ILogicallyCombinable<Condition> Condition, CombinationalBehavior Behavior)> ConditionMappings { get; } = [];

    /// <summary>
    /// Generate new dynamic behavior
    /// </summary>
    public DynamicBehavior()
    {
        ConditionMappings.CollectionChanged += CasesListUpdated;
    }

    /// <inheritdoc/>
    public override IEnumerable<NamedSignal> NamedInputSignals => ConditionMappings.SelectMany(c => c.Behavior.NamedInputSignals.Union(c.Condition.BaseObjects.SelectMany(c => c.InputSignals).Where(s => s is NamedSignal).Select(s => (NamedSignal)s))).Distinct();

    /// <inheritdoc/>
    public override Dimension Dimension => Dimension.CombineWithoutCheck(ConditionMappings.Select(c => c.Behavior.Dimension));

    /// <inheritdoc/>
    public override string ToVhdl(NamedSignal outputSignal)
    {
        if (ConditionMappings.Count == 0)
            throw new Exception("Must have at least one condition mapping");
        
        StringBuilder sb = new();
        sb.AppendLine($"process({string.Join(", ", NamedInputSignals.Select(s => s.Name))}) is");
        sb.AppendLine("begin");

        // First condition
        (ILogicallyCombinable<Condition> firstCondition, CombinationalBehavior firstBehavior) = ConditionMappings.First();
        sb.AppendLine($"\tif ({firstCondition.ToLogicString()}) then");
        sb.AppendLine(firstBehavior.ToVhdl(outputSignal).AddIndentation(2));

        // Remaining conditions
        foreach ((ILogicallyCombinable<Condition> condition, CombinationalBehavior behavior) in ConditionMappings.Skip(1))
        {
            sb.AppendLine($"\telse if ({condition.ToLogicString()}) then");
            sb.AppendLine(behavior.ToVhdl(outputSignal).AddIndentation(2));
        }

        sb.AppendLine("\tend if;");
        sb.AppendLine("end process;");

        return sb.ToString();
    }

    /// <inheritdoc/>
    protected override void CheckValid()
    {
        // Check parent modules
        base.CheckValid();
        // Check that dimensions of all behaviors are compatible
        if (!Dimension.AreCompatible(ConditionMappings.Select(c => c.Behavior.Dimension)))
            throw new Exception("Expressions are incompatible. Must have same or compatible dimensions");
    }

    private void CasesListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckValid();
    }

    /// <inheritdoc/>
    public override string ToSpice(NamedSignal outputSignal, string uniqueId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override IEnumerable<IEntity> GetSpiceSharpEntities(NamedSignal outputSignal, string uniqueId)
    {
        throw new NotImplementedException();
    }
}