using VHDLSharp.Behaviors;
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
    public override INamedSignal this[Range range] =>
        range.Start.GetOffset(1) == 0 && range.End.GetOffset(1) == 1 ? this :
        throw new ArgumentOutOfRangeException(nameof(range), "The range for slicing a single-node signal must be 0");

    /// <inheritdoc/>
    public override bool CanCombine(ILogicallyCombinable<ISignal> other) => ISignal.CanCombineSignals(this, other);

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
    public void AssignBehavior(bool value) => this.AssignBehavior(new ValueBehavior(value ? 1 : 0));
}