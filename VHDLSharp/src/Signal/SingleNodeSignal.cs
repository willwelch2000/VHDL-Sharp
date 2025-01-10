using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Base class for any signal that contains just a single node (not a vector)
/// </summary>
public abstract class SingleNodeSignal : NamedSignal
{
    /// <inheritdoc/>
    public override abstract string Name { get; }

    /// <inheritdoc/>
    public override abstract Module ParentModule { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => new(1);

    /// <inheritdoc/>
    public override string VhdlType => "std_logic";

    /// <inheritdoc/>
    public override string ToVhdl => $"signal {Name}\t: {VhdlType}";

    /// <inheritdoc/>
    public override IEnumerable<ISignal> BaseObjects => [this];

    /// <inheritdoc/>
    public override bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        // If there's a signal with a parent, check that one--otherwise, get the first available
        ISignal? signal = other.BaseObjects.FirstOrDefault(e => e.ParentModule is not null) ?? other.BaseObjects.FirstOrDefault();
        if (signal is null)
            return true;
        // Fine if dimension is compatible and parent is null or compatible
        return Dimension.Compatible(signal.Dimension) && (signal.ParentModule is null || ParentModule == signal.ParentModule);
    }

    /// <inheritdoc/>
    public override string ToLogicString() => Name;

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override IEnumerable<SingleNodeSignal> ToSingleNodeSignals => [this];

    /// <summary>
    /// Get representation in SPICE
    /// </summary>
    /// <returns></returns>
    public abstract string ToSpice();
}