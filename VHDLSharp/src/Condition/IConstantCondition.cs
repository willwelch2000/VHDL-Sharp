using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Conditions;

/// <summary>
/// A <see cref="ICondition"/> that can be true for extended periods of time. 
/// For example, an equality comparison
/// </summary>
public interface IConstantCondition : ICondition
{
    /// <summary>
    /// Get a <see cref="SpiceCircuit"/> that produces an output signal 
    /// corresponding to the boolean value of the condition. The output signal
    /// should be high whenever the condition is true. 
    /// </summary>
    /// <param name="uniqueId"></param>
    /// <param name="outputSignal"></param>
    /// <returns></returns>
    public SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal);
}