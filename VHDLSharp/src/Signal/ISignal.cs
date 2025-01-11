using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any type of signal that can be used in an expression
/// Defined by a dimension and an optional parent module
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal>
{
    /// <summary>
    /// Object explaining many nodes are part of this signal (1 for normal signal)
    /// </summary>
    public DefiniteDimension Dimension { get; }

    /// <summary>
    /// Module this signal belongs to, if applicable
    /// </summary>
    public Module? ParentModule { get; }

    /// <summary>
    /// If this has a dimension > 1, convert to a list of things with dimension 1
    /// If it is dimension 1, then return itself
    /// </summary>
    public IEnumerable<ISingleNodeSignal> ToSingleNodeSignals { get; }

    /// <summary>
    /// Indexer for multi-dimensional signals
    /// A single-dimensional signal will just return itself for the first item
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ISingleNodeSignal this[int index] { get; }
}