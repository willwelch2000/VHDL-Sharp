using System.Collections.ObjectModel;
using System.Text;
using VHDLSharp.LogicTree;
using VHDLSharp.Utility;
using VHDLSharp.Conditions;
using VHDLSharp.Signals;
using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
using VHDLSharp.Validation;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Behavior that uses sequential rather than combinational logic. 
/// Maps <see cref="Condition"/> objects to <see cref="ICombinationalBehavior"/> objects. 
/// The output signal is assigned the value from the combinational behavior when the condition is met. 
/// Priority is used for the conditions
/// </summary>
public class DynamicBehavior : Behavior
{
    /// <summary>
    /// Ordered mapping of condition to behavior
    /// </summary>
    private ObservableCollection<(ILogicallyCombinable<ICondition> Condition, ICombinationalBehavior Behavior)> ConditionMappings { get; } = [];

    /// <summary>
    /// Generate new dynamic behavior
    /// </summary>
    public DynamicBehavior()
    {
        ConditionMappings.CollectionChanged += InvokeBehaviorUpdated;
    }

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> NamedInputSignals => ConditionMappings.SelectMany(c => c.Behavior.NamedInputSignals.Union(c.Condition.BaseObjects.SelectMany(c => c.InputSignals).OfType<INamedSignal>())).Distinct();

    /// <inheritdoc/>
    public override Dimension Dimension => Dimension.CombineWithoutCheck(ConditionMappings.Select(c => c.Behavior.Dimension));

    /// <inheritdoc/>
    public override string GetVhdlStatement(INamedSignal outputSignal)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Dynamic behavior must be valid to convert to VHDL");
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        if (ConditionMappings.Count == 0)
            throw new Exception("Must have at least one condition mapping");
        
        StringBuilder sb = new();
        sb.AppendLine($"process({string.Join(", ", NamedInputSignals.Select(s => s.Name))}) is");
        sb.AppendLine("begin");

        // First condition
        (ILogicallyCombinable<ICondition> firstCondition, ICombinationalBehavior firstBehavior) = ConditionMappings.First();
        sb.AppendLine($"\tif ({firstCondition.ToLogicString()}) then");
        sb.AppendLine(firstBehavior.GetVhdlStatement(outputSignal).AddIndentation(2));

        // Remaining conditions
        foreach ((ILogicallyCombinable<ICondition> condition, ICombinationalBehavior behavior) in ConditionMappings.Skip(1))
        {
            sb.AppendLine($"\telse if ({condition.ToLogicString()}) then");
            sb.AppendLine(behavior.GetVhdlStatement(outputSignal).AddIndentation(2));
        }

        sb.AppendLine("\tend if;");
        sb.AppendLine("end process;");

        return sb.ToString();
    }

    /// <inheritdoc/>
    protected override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        // Check parent modules
        base.CheckTopLevelValidity(out exception);

        // Check that dimensions of all behaviors are compatible
        if (!Dimension.AreCompatible(ConditionMappings.Select(c => c.Behavior.Dimension)))
            exception = new Exception("Expressions are incompatible. Must have same or compatible dimensions");

        return exception is null;
    }

    /// <inheritdoc/>
    public override SpiceCircuit GetSpice(INamedSignal outputSignal, string uniqueId)
    {
        throw new NotImplementedException();
    }
}