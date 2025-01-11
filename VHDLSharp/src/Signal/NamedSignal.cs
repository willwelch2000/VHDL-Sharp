using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Single-node and vector signals that are contained in a module and have a name
/// </summary>
public abstract class NamedSignal : ISignal
{
    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public abstract Module ParentModule { get; }

    /// <summary>
    /// Name of the signal
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Type of signal as VHDL
    /// </summary>
    public abstract string VhdlType { get; }

    /// <summary>
    /// Get signal as VHDL
    /// TODO might should be renamed
    /// </summary>
    /// <returns></returns>
    public abstract string ToVhdl { get; }

    /// <summary>
    /// Dimension of signal with definite value
    /// Of type <see cref="DefiniteDimension"/>
    /// </summary>
    public abstract DefiniteDimension Dimension { get; }

    /// <inheritdoc/>
    public abstract IEnumerable<ISignal> BaseObjects { get; }

    /// <inheritdoc/>
    public abstract bool CanCombine(ILogicallyCombinable<ISignal> other);

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <inheritdoc/>
    public IEnumerable<ISingleNodeSignal> ToSingleNodeSignals => ToSingleNodeNamedSignals;

    /// <summary>
    /// If this has a dimension > 1, convert to a list of named signals with dimension 1
    /// If it is dimension 1, then return itself
    /// More type-specific version of <see cref="ToSingleNodeSignals"/>
    /// </summary>
    public abstract IEnumerable<SingleNodeNamedSignal> ToSingleNodeNamedSignals { get; }

    ISingleNodeSignal ISignal.this[int index] => this[index];

    /// <summary>
    /// Indexer for multi-dimensional signals
    /// A single-dimensional signal will just return itself for the first item
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual SingleNodeNamedSignal this[int index]
    {
        get
        {
            if (index < 0 || index >= Dimension.NonNullValue)
                throw new ArgumentOutOfRangeException(nameof(index), $"Must be between 0 and dimension ({Dimension.NonNullValue})");
            return ToSingleNodeNamedSignals.ElementAt(index);
        }
    }
}