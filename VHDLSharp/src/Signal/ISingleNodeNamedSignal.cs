using VHDLSharp.Conditions;

namespace VHDLSharp.Signals;

/// <summary>
/// Base class for any signal that contains just a single node (not a vector).
/// </summary>
public interface ISingleNodeNamedSignal : INamedSignal, ISingleNodeSignal
{
    /// <summary>Get rising edge condition for this signal</summary>
    public RisingEdge RisingEdge() => new(this);
    
    /// <summary>Get falling edge condition for this signal</summary>
    public FallingEdge FallingEdge() => new(this);

    /// <summary>Get high condition for this signal</summary>
    public High IsHigh() => new(this);

    /// <summary>Get low condition for this signal</summary>
    public Low IsLow() => new(this);
}