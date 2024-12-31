using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// Condition that can be used in an if statement
/// </summary>
public abstract class Condition : ILogicallyCombinable<Condition>
{
    /// <inheritdoc/>
    public IEnumerable<Condition> BaseObjects => [this];

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<Condition> other)
    {
        Condition? otherCondition = other.BaseObjects.FirstOrDefault(c => c.ParentModule is not null);
        return otherCondition is null || otherCondition.ParentModule == ParentModule;
    }

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <summary>
    /// Get parent module based on named input signals
    /// </summary>
    public Module? ParentModule => InputSignals.FirstOrDefault(s => s.ParentModule is not null)?.ParentModule;

    /// <summary>
    /// Input signals to condition
    /// </summary>
    public abstract IEnumerable<ISignal> InputSignals { get; }
}