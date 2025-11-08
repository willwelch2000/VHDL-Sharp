using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;

namespace VHDLSharp.Signals;

/// <summary>
/// A bit in a literal
/// </summary>
public class LiteralNode : ISingleNodeSignal, ISignalWithKnownValue
{
    /// <summary>
    /// Generate new <see cref="LiteralNode"/> given parent <see cref="Literal"/> and node index
    /// </summary>
    /// <param name="literal"></param>
    /// <param name="node"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal LiteralNode(Literal literal, int node)
    {
        Literal = literal;
        Node = node;
        if (node >= literal.Dimension.NonNullValue)
            throw new ArgumentOutOfRangeException(nameof(node), "Node is higher than literal's dimension allows");
    }

    /// <summary>
    /// The Literal that this belongs to
    /// </summary>
    public Literal Literal { get; }

    /// <summary>
    /// Which node this is in the literal
    /// </summary>
    public int Node { get; }

    /// <summary>
    /// Value of bit in literal
    /// </summary>
    public bool Value => (Literal.Value & 1<<Node) > 0;

    int ISignalWithKnownValue.Value => Value ? 1 : 0;

    /// <inheritdoc/>
    public DefiniteDimension Dimension => new(1);

    /// <inheritdoc/>
    public IEnumerable<LiteralNode> ToSingleNodeSignals => [this];

    /// <summary>
    /// Parent signal
    /// </summary>
    public Literal? ParentSignal => Literal;

    /// <summary>
    /// Top level signal
    /// </summary>
    public Literal TopLevelSignal => Literal;

    ISignal? ISignal.ParentSignal => ParentSignal;

    ISignal ISignal.TopLevelSignal => TopLevelSignal;

    /// <inheritdoc/>
    public IEnumerable<ISignal> ChildSignals => [];

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        return Dimension.Compatible(Dimensions.Dimension.CombineWithoutCheck(other.BaseObjects.Select(o => o.Dimension)));
    }

    /// <inheritdoc/>
    public bool CanCombine(IEnumerable<ILogicallyCombinable<ISignal>> others) => ISignal.CanCombineSignals([this, .. others]);

    /// <inheritdoc/>
    public string GetVhdlName() => Value.ToString();

    /// <inheritdoc/>
    public string ToLogicString() => GetVhdlName();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <summary>
    /// Power (VDD) if high bit, ground otherwise
    /// </summary>
    /// <returns></returns>
    public string GetSpiceName() => Value ? "VDD" : "0";
}