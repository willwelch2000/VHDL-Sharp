using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// Interface for any type of signal that can be used in an expression
/// Defined by a dimension and a string expression of the signal
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal>
{
    /// <summary>
    /// Object explaining many nodes are (or can be) part of this signal (1 for normal signal)
    /// </summary>
    public Dimension Dimension { get; }
    
    /// <summary>
    /// Representation of signal in expression given dimension to match
    /// Dimension is needed for flexible signals, such as literals
    /// </summary>
    /// <param name="dimension"></param>
    /// <returns></returns>
    public string ToVhdlInExpression(DefiniteDimension dimension);
}