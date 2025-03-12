using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;

namespace VHDLSharp.Conditions;

/// <summary>
/// Interface for a condition that can be combined with others and used in a dynamic behavior
/// </summary>
public interface ICondition : ILogicallyCombinable<ICondition>
{
    /// <summary>
    /// Get parent module, if it exists
    /// </summary>
    public IModule? ParentModule { get; }

    /// <summary>
    /// Input signals to condition
    /// </summary>
    public IEnumerable<ISignal> InputSignals { get; }
}