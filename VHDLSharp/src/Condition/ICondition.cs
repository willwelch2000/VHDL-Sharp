using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;

namespace VHDLSharp.Conditions;

// As of now, there are no conditions that allow sub-conditions. If one is added,
// just don't have a setter for it--this ensures that sub-conditions can't be recursive
/// <summary>
/// Interface for a condition that can be combined with others and used in a dynamic behavior. 
/// Recursion with conditions is not supported or checked for, so it should not be allowed by 
/// implementing classes. This can be ensured by not allowing sub-conditions to be switched out
/// after construction. 
/// </summary>
public interface ICondition : ILogicallyCombinable<ICondition>
{
    /// <summary>
    /// Get parent module, if it exists
    /// </summary>
    public IModule? ParentModule { get; }

    /// <summary>
    /// Module-specific input signals to condition
    /// </summary>
    public IEnumerable<IModuleSpecificSignal> InputModuleSignals { get; }

    /// <summary>
    /// Determine if the condition is met given the rule-based simulation state. 
    /// Should evaluate as true/false based on the previous time step
    /// </summary>
    /// <param name="state"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public bool Evaluate(RuleBasedSimulationState state, SubmoduleReference context);
}