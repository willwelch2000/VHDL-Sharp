using VHDLSharp.Behaviors;
using VHDLSharp.Conditions;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Base class for any signal that contains just a single node (dimension of 1)
/// </summary>
public abstract class SingleNodeNamedSignal : NamedSignal, ISingleNodeNamedSignal
{
    /// <inheritdoc/>
    public override abstract string Name { get; }

    /// <inheritdoc/>
    public override abstract IModule ParentModule { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => new(1);

    /// <inheritdoc/>
    public override string VhdlType => "std_logic";

    /// <inheritdoc/>
    public override IEnumerable<SingleNodeNamedSignal> ChildSignals => [];

    /// <inheritdoc/>
    public override IEnumerable<SingleNodeNamedSignal> ToSingleNodeSignals => [this];

    /// <inheritdoc/>
    public override bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        // If there's a signal with a parent, check that one--otherwise, get the first available
        ISignal? signal = other.BaseObjects.FirstOrDefault(e => e is INamedSignal) ?? other.BaseObjects.FirstOrDefault();
        if (signal is null)
            return true;
        // Fine if dimension is compatible and parent is null or compatible
        return Dimension.Compatible(signal.Dimension) && (signal is not INamedSignal namedSignal || ParentModule.Equals(namedSignal.ParentModule));
    }

    /// <inheritdoc/>
    public override string GetVhdlName() => Name;

    /// <inheritdoc/>
    public override string ToLogicString() => Name;

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public abstract string GetSpiceName();

    /// <summary>
    /// Assign a specified value to the signal as a <see cref="ValueBehavior"/>
    /// </summary>
    /// <param name="value"></param>
    public void AssignBehavior(bool value) => AssignBehavior(new ValueBehavior(value ? 1 : 0));

    /// <inheritdoc/>
    public RisingEdge RisingEdge() => new(this);
    
    /// <inheritdoc/>
    public FallingEdge FallingEdge() => new(this);

    /// <inheritdoc/>
    public High IsHigh() => new(this);

    /// <inheritdoc/>
    public Low IsLow() => new(this);
}