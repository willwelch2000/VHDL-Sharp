using VHDLSharp.LogicTree;

namespace VHDLSharp;

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
    public override DefiniteDimension DefiniteDimension => new(1);

    /// <inheritdoc/>
    public override string VhdlType => "std_logic";

    /// <inheritdoc/>
    public override string ToVhdl => $"signal {Name}\t: {VhdlType}";

    /// <inheritdoc/>
    public override IEnumerable<ISignal> BaseObjects => [this];

    /// <inheritdoc/>
    public override bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        // If there's a named signal (with a parent), check that one--otherwise, get the first available
        ISignal? signal = other.BaseObjects.FirstOrDefault(e => e is NamedSignal) ?? other.BaseObjects.FirstOrDefault();
        if (signal is null)
            return true;
        // Fine if dimension is compatible and parent is null or compatible
        return Dimension.Compatible(signal.Dimension) && (signal is not NamedSignal namedSignal || ParentModule == namedSignal.ParentModule);
    }

    /// <inheritdoc/>
    public override string ToLogicString() => Name;

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override string ToVhdlInExpression(DefiniteDimension dimension) => Name;
}