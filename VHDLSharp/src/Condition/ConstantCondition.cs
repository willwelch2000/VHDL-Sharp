using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Conditions;

/// <summary>
/// Condition that can be used in a dynamic behavior
/// </summary>
public abstract class ConstantCondition : Condition, IConstantCondition
{
    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<IConstantCondition> other)
    {
        ICondition? otherCondition = other.BaseObjects.FirstOrDefault(c => c.ParentModule is not null);
        return otherCondition is null || (otherCondition.ParentModule?.Equals(ParentModule) ?? false);
    }

    /// <inheritdoc/>
    public abstract SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal);
}