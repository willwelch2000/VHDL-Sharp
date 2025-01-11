using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// A bit in a literal
/// </summary>
public class LiteralNode : ISingleNodeSignal
{
    /// <summary>
    /// Generate new <see cref="LiteralNode"/> given parent <see cref="Literal"/> and node index
    /// </summary>
    /// <param name="literal"></param>
    /// <param name="node"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public LiteralNode(Literal literal, int node)
    {
        Literal = literal;
        Node = node;
        if (node >= literal.Dimension.NonNullValue)
            throw new ArgumentOutOfRangeException(nameof(node), "Node is higher than literal's dimension allows");
    }

    /// <summary>
    /// Indexer--must be 0 for this
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ISingleNodeSignal this[int index]
    {
        get
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException(nameof(index), $"Must be 0 for single node signal");
            return this;
        }
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

    /// <inheritdoc/>
    public DefiniteDimension Dimension => new(1);

    /// <inheritdoc/>
    public Module? ParentModule => null;

    /// <inheritdoc/>
    public IEnumerable<ISingleNodeSignal> ToSingleNodeSignals => [this];

    /// <inheritdoc/>
    public IEnumerable<ISignal> BaseObjects => [this];

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        return Dimension.Compatible(Dimensions.Dimension.CombineWithoutCheck(other.BaseObjects.Select(o => o.Dimension)));
    }

    /// <inheritdoc/>
    public string ToLogicString() => Value.ToString();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public string ToSpice()
    {
        throw new NotImplementedException();
    }
}