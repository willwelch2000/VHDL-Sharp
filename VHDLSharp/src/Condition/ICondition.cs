using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;

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

    /// <summary>
    /// Determine if the condition is met given the rule-based simulation state. 
    /// Should evaluate as true based on the previous time step
    /// </summary>
    /// <param name="state"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public bool Evaluate(RuleBasedSimulationState state, SubcircuitReference context);
}