using VHDLSharp.Signals;

namespace VHDLSharp;

/// <summary>
/// Interface for something that allows some of its input signals to be circularly produced
/// </summary>
public interface IAllowCircularSignals
{
    // Originally I wanted to have this be the allowed signals, but that strategy is 
    // difficult to implement if an input signal is in multiple places 
    /// <summary>
    /// The input signals that cannot be circularly produced
    /// </summary>
    public IEnumerable<IModuleSpecificSignal> DisallowedCircularSignals { get; }
}