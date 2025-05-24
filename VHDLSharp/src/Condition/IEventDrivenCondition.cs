using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Conditions;

/// <summary>
/// A <see cref="ICondition"/> that can only be true instantaneously. 
/// For example, a rising-edge condition
/// </summary>
public interface IEventDrivenCondition : ICondition
{
    /// <summary>
    /// Get a <see cref="SpiceCircuit"/> that produces an output signal 
    /// corresponding to the boolean value of the condition. The signal should
    /// act as a load signal that goes high at the moment of the given event. 
    /// </summary>
    /// <param name="uniqueId"></param>
    /// <param name="outputSignal"></param>
    /// <returns></returns>
    public SpiceCircuit GetSpiceCircuit(string uniqueId, ISingleNodeNamedSignal outputSignal);
}