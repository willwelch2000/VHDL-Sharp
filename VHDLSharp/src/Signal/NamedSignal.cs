using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Behaviors;
using VHDLSharp.Conditions;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Single-node and vector signals that are contained in a module and have a name
/// </summary>
public abstract class NamedSignal : INamedSignal, IEquatable<INamedSignal>
{
    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public abstract IModule ParentModule { get; }

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
    public abstract INamedSignal? ParentSignal { get; }

    /// <summary>
    /// Top-level signal of this
    /// </summary>
    public abstract INamedSignal TopLevelSignal { get; }

    ISignal ISignal.TopLevelSignal => TopLevelSignal;

    ISignal? ISignal.ParentSignal => ParentSignal;

    /// <summary>
    /// Child signals of this
    /// </summary>
    public abstract IEnumerable<INamedSignal> ChildSignals { get; }

    IEnumerable<ISignal> ISignal.ChildSignals => ChildSignals;

    /// <summary>
    /// If this has a dimension > 1, convert to a list of named signals with dimension 1
    /// If it is dimension 1, then return itself
    /// </summary>
    public abstract IEnumerable<ISingleNodeNamedSignal> ToSingleNodeSignals { get; }

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;

    /// <summary>
    /// Behavior assigned to this signal in its module
    /// </summary>
    public IBehavior? Behavior
    {
        get => ParentModule.SignalBehaviors.TryGetValue(this, out IBehavior? value) ? value : null;
        set
        {
            if (value is null)
                RemoveBehavior();
            else
                AssignBehavior(value);
        }
    }

    /// <summary>
    /// Indexer for multi-dimensional signals
    /// A single-dimensional signal will just return itself for the first item
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public virtual ISingleNodeNamedSignal this[Index index]
    {
        get
        {
            int actualIndex = index.IsFromEnd ? Dimension.NonNullValue - index.Value : index.Value; // From ChatGPT
            if (actualIndex < 0 || actualIndex >= Dimension.NonNullValue)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index ({actualIndex}) must refer to a node between 0 and {Dimension.NonNullValue - 1}");
            return ToSingleNodeSignals.ElementAt(actualIndex);
        }
    }

    /// <inheritdoc/>
    public abstract INamedSignal this[Range range] { get; }

    ISingleNodeSignal ISignal.this[Index index] => this[index];

    /// <inheritdoc/>
    public abstract bool CanCombine(ILogicallyCombinable<ISignal> other);

    /// <inheritdoc/>
    public bool CanCombine(IEnumerable<ILogicallyCombinable<ISignal>> others) => ISignal.CanCombineSignals([this, .. others]);

    /// <inheritdoc/>
    public abstract string GetVhdlName();

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <inheritdoc/>
    public override string ToString() => Name;

    /// <summary>
    /// Get signal declaration as VHDL
    /// </summary>
    /// <returns></returns>
    public virtual string GetVhdlDeclaration() => $"signal {Name}\t: {VhdlType}";

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

    // Methods that are shortcuts for adding behaviors

    /// <summary>
    /// Assign a specified behavior to the signal
    /// </summary>
    /// <param name="behavior"></param>
    /// <returns>Assigned behavior</returns>
    public IBehavior AssignBehavior(IBehavior behavior)
    {
        ParentModule.SignalBehaviors[this] = behavior;
        return behavior;
    }

    /// <summary>
    /// Assign a specified value to the signal as a <see cref="ValueBehavior"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Assigned behavior</returns>
    public IBehavior AssignBehavior(int value) => AssignBehavior(new ValueBehavior(value));

    /// <summary>
    /// Assign a specified expression to the signal as a <see cref="LogicBehavior"/>
    /// </summary>
    /// <param name="expression"></param>
    /// <returns>Assigned behavior</returns>
    public IBehavior AssignBehavior(ILogicallyCombinable<ISignal> expression) => AssignBehavior(new LogicBehavior(expression));

    /// <summary>
    /// Remove behavior assignment from this signal
    /// </summary>
    public void RemoveBehavior()
    {
        ParentModule.SignalBehaviors.Remove(this);
    }

    /// <inheritdoc/>
    public virtual bool IsPartOfPortMapping(PortMapping mapping, [MaybeNullWhen(false)] out INamedSignal equivalentSignal)
    {
        IPort? port = mapping.Keys.FirstOrDefault(p => p.Signal == this);
        equivalentSignal = port is null ? null : mapping[port];
        return equivalentSignal is not null;
    }

    /// <inheritdoc/>
    public bool Equals(INamedSignal? other)
    {
        if (other is null)
            return false;
        ISingleNodeNamedSignal[] children = [.. ToSingleNodeSignals];
        ISingleNodeNamedSignal[] otherChildren = [.. other.ToSingleNodeSignals];
        return children.Length == otherChildren.Length && children.Zip(otherChildren).All(pair => pair.First == pair.Second);
    }

    /// <summary>Convert signal to <see cref="LogicBehavior"/></summary>
    /// <param name="signal">Signal to convert</param>
    public static implicit operator LogicBehavior(NamedSignal signal) => new(signal);

    /// <summary>
    /// Create a named signal with the given dimension. 
    /// Produces a <see cref="Signal"/> if the dimension is 1, otherwise a <see cref="Vector"/>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parentModule"></param>
    /// <param name="dimension"></param>
    /// <returns></returns>
    /// <exception cref="Exception">If dimension is less than 1</exception>
    public static ITopLevelNamedSignal GenerateSignalOrVector(string name, IModule parentModule, int dimension) => dimension switch
    {
        1 => new Signal(name, parentModule),
        > 1 => new Vector(name, parentModule, dimension),
        _ => throw new Exception("Dimension must be >= 0"),
    };
}