using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Utility;

namespace VHDLSharp.Signals;

/// <summary>
/// Literal value that can be used in expressions
/// </summary>
public class Literal : ISignalWithKnownValue, IEquatable<Literal>
{
    private readonly LiteralNode[] bits;

    /// <summary>
    /// Generate new literal given value
    /// </summary>
    /// <param name="value">Must be >= 0</param>
    /// <param name="dimension">Must be >= 0</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Literal(int value, int dimension)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 0");

        Value = value;

        if (value == 0)
        {
            if (dimension < 1)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Must be >= 0");
        }
        else
        {
            double min = Math.Floor(Math.Log2(value)) + 1;
            if (dimension < min)
                throw new ArgumentOutOfRangeException(nameof(dimension), "Must be large enough for value");
        }

        Dimension = new(dimension);

        bits = [.. Enumerable.Range(0, dimension).Select(i => new LiteralNode(this, i))];
    }

    /// <summary>
    /// Literal value
    /// </summary>
    public int Value { get; }

    /// <inheritdoc/>
    public DefiniteDimension Dimension { get; }

    /// <summary>
    /// Top-level signal is just this object
    /// </summary>
    public Literal TopLevelSignal => this;

    ISignal ISignal.TopLevelSignal => TopLevelSignal;

    /// <summary>
    /// Get the single-node signals that make up this
    /// </summary>
    public IEnumerable<LiteralNode> ToSingleNodeSignals => [.. bits];

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;

    /// <inheritdoc/>
    public ISignal? ParentSignal => null;

    /// <summary>
    /// Get the single-node signals that make up this
    /// </summary>
    public IEnumerable<LiteralNode> ChildSignals => [.. bits];

    IEnumerable<ISignal> ISignal.ChildSignals => ChildSignals;

    /// <summary>
    /// Access individual node signals of literal
    /// These can be used as single-node signals
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LiteralNode this[Index index]
    {
        get
        {
            int actualIndex = index.IsFromEnd ? Dimension.NonNullValue - index.Value : index.Value; // From ChatGPT
            if (actualIndex < 0 || actualIndex >= Dimension.NonNullValue)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index ({actualIndex}) must refer to a node between 0 and {Dimension.NonNullValue - 1}");
            return bits[actualIndex];
        }
    }

    ISingleNodeSignal ISignal.this[Index index] => this[index];

    /// <summary>
    /// Just check dimension since this has no parent module
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        return Dimension.Compatible(Dimensions.Dimension.CombineWithoutCheck(other.BaseObjects.Select(o => o.Dimension)));
    }

    /// <inheritdoc/>
    public bool CanCombine(IEnumerable<ILogicallyCombinable<ISignal>> others) => ISignal.CanCombineSignals([this, .. others]);

    /// <inheritdoc/>
    public string GetVhdlName() => $"\"{Value.ToBinaryString(Dimension.NonNullValue)}\"";

    /// <inheritdoc/>
    public string ToLogicString() => GetVhdlName();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => ToLogicString();
    
    /// <inheritdoc/>
    public bool Equals(Literal? other) =>
        other is not null && other.Dimension.NonNullValue.Equals(Dimension.NonNullValue) && other.Value == Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Literal);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode code = new();
        code.Add(Dimension.NonNullValue);
        code.Add(Value);
        return code.ToHashCode();
    }
}