using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq.Expressions;
using VHDLSharp.LogicTree;

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
    private readonly ObservableCollection<(ILogicallyCombinable<Condition> condition, CombinationalBehavior behavior)> conditionMappings = [];

    /// <summary>
    /// Generate new dynamic behavior
    /// </summary>
    public DynamicBehavior()
    {
        conditionMappings.CollectionChanged += CasesListUpdated;
    }

    /// <inheritdoc/>
    public override IEnumerable<ISignal> InputSignals => conditionMappings.SelectMany(c => c.behavior.InputSignals).Distinct();

    /// <inheritdoc/>
    public override int? Dimension => conditionMappings.Any() ? conditionMappings.First().behavior.Dimension : null;

    /// <inheritdoc/>
    public override string ToVhdl(ISignal outputSignal) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void CheckValid()
    {
        base.CheckValid(); // Checks that everything is in just one module
        // Go through all after first and test compatibility with first
        if (conditionMappings.Count > 1)
        {
            (ILogicallyCombinable<Condition> condition, CombinationalBehavior behavior) first = conditionMappings.First();
            foreach ((ILogicallyCombinable<Condition> condition, CombinationalBehavior behavior) in conditionMappings.Skip(1))
                if (first.behavior.Dimension is not null && first.behavior.Dimension != behavior.Dimension)
                    throw new Exception("Expressions are incompatible. Must have same dimension");
        }
    }

    private void CasesListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CheckValid();
    }
}