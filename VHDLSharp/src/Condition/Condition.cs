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
        return other.BaseObjects.First().Module == Module;
    }

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <summary>
    /// Get parent module based on input signals
    /// </summary>
    public Module Module => InputSignals.First().Parent;

    /// <inheritdoc/>
    public abstract IEnumerable<ISignal> InputSignals { get; }
}