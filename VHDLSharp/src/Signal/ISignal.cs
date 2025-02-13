using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any type of signal that can be used in an expression
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal>
{
    /// <summary>
    /// Object explaining how many nodes are part of this signal (1 for normal signal)
    /// </summary> 
    public DefiniteDimension Dimension { get; }

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
    /// If this is the top level, it returns this. 
    /// Otherwise, it goes up in hierarchy as much as possible
    /// </summary>
    public ISignal TopLevelSignal
    {
        get
        {
            ISignal signal = this;
            while (signal.ParentSignal is not null)
                signal = signal.ParentSignal;
            return signal;
        }
    }

    /// <summary>
    /// If this has children (e.g. vector), get the child signals
    /// </summary>
    public IEnumerable<ISignal> ChildSignals { get; }

    /// <summary>
    /// If this has a dimension > 1, convert to a list of things with dimension 1. 
    /// If it is dimension 1, then return itself
    /// </summary>
    public IEnumerable<ISingleNodeSignal> ToSingleNodeSignals
    {
        get
        {
            if (ChildSignals.Any())
                return ChildSignals.SelectMany(s => s.ToSingleNodeSignals);
            return this is ISingleNodeSignal singleNodeSignal ? [singleNodeSignal] : [];
        }
    }

    /// <summary>
    /// Given several signals, returns true if they can be combined together
    /// </summary>
    /// <param name="combinables"></param>
    /// <returns></returns>
    internal static bool CanCombineSignals(IEnumerable<ILogicallyCombinable<ISignal>> combinables)
    {
        IEnumerable<ISignal> baseSignals = combinables.SelectMany(c => c.BaseObjects);

        // 1 or 0 signals is always true
        if (baseSignals.Count() < 2)
            return true;

        // Find named signal, if it exists
        INamedSignal? namedSignal = baseSignals.FirstOrDefault(s => s is INamedSignal) as INamedSignal;
        if (namedSignal is not null)
        {
            // If any signal has another parent
            if (baseSignals.Any(s => s is INamedSignal namedS && namedS.ParentModule != namedSignal.ParentModule))
                return false;
        }

        // If any signal has incompatible dimension with first
        ISignal first = baseSignals.First();
        if (baseSignals.Any(s => !s.Dimension.Compatible(first.Dimension)))
            return false;

        return true;
    }
}