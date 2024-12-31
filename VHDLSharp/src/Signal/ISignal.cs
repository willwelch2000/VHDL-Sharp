using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// Interface for any type of signal that can be used in an expression
/// Defined by a dimension and a string expression of the signal
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal>
{
    /// <summary>
    /// Object explaining many nodes are part of this signal (1 for normal signal)
    /// </summary>
    public DefiniteDimension Dimension { get; }
}