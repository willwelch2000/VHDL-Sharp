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
    /// Dimension of signal with definite value. 
    /// Of type <see cref="DefiniteDimension"/>
    /// </summary>
    public abstract DefiniteDimension Dimension { get; }

    /// <summary>
    /// Parent signal of this, if it exists
    /// </summary>
    public abstract NamedSignal? ParentSignal { get; }

    /// <summary>
    /// Top-level signal of this
    /// </summary>
    public abstract NamedSignal TopLevelSignal { get; }

    ISignal ISignal.TopLevelSignal => TopLevelSignal;

    ISignal? ISignal.ParentSignal => ParentSignal;

    /// <summary>
    /// Child signals of this
    /// </summary>
    public abstract IEnumerable<NamedSignal> ChildSignals { get; }

    IEnumerable<ISignal> ISignal.ChildSignals => ChildSignals;

    /// <summary>
    /// If this has a dimension > 1, convert to a list of named signals with dimension 1
    /// If it is dimension 1, then return itself
    /// </summary>
    public abstract IEnumerable<SingleNodeNamedSignal> ToSingleNodeSignals { get; }

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;

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
            return ToSingleNodeSignals.ElementAt(index);
        }
    }

    ISingleNodeSignal ISignal.this[int index] => this[index];

    /// <inheritdoc/>
    public abstract bool CanCombine(ILogicallyCombinable<ISignal> other);

    /// <inheritdoc/>
    public bool CanCombine(IEnumerable<ILogicallyCombinable<ISignal>> others) => ISignal.CanCombineSignals([this, .. others]);

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>
    /// Get signal as VHDL
    /// </summary>
    /// <returns></returns>
    public abstract string ToVhdl();

    // The following functions are given here so that they can be accessed without referring to this object as ISignal
    
    /// <inheritdoc/>
    public And<ISignal> And(ILogicallyCombinable<ISignal> other) => new(this, other);

    /// <inheritdoc/>
    public And<ISignal> And(IEnumerable<ILogicallyCombinable<ISignal>> others) => new([this, .. others]);

    /// <inheritdoc/>
    public Or<ISignal> Or(ILogicallyCombinable<ISignal> other) => new(this, other);

    /// <inheritdoc/>
    public Or<ISignal> Or(IEnumerable<ILogicallyCombinable<ISignal>> others) => new([this, .. others]);

    /// <inheritdoc/>
    public Not<ISignal> Not() => new(this);
}