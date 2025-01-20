using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any type of signal that can be used in an expression
/// Defined by a dimension and an optional parent module
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal>, IHasParentModule
{
    /// <summary>
    /// Object explaining many nodes are part of this signal (1 for normal signal)
    /// </summary>
    public DefiniteDimension Dimension { get; }

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

    /// <summary>
    /// If this is part of a larger group (e.g. vector node), get the parent signal (one layer up)
    /// </summary>
    public ISignal? ParentSignal { get; }

    /// <summary>
    /// If this is the top level, it returns this
    /// Otherwise, it goes up in hierarchy as much as possible
    /// </summary>
    public ISignal TopLevelSignal { get; }

    /// <summary>
    /// If this has children (e.g. vector), get the child signals
    /// </summary>
    public IEnumerable<ISignal> ChildSignals { get; }
}