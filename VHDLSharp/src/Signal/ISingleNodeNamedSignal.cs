namespace VHDLSharp.Signals;

/// <summary>
/// Base class for any signal that contains just a single node (not a vector).
/// </summary>
public interface ISingleNodeNamedSignal : INamedSignal, ISingleNodeSignal
{
    
}