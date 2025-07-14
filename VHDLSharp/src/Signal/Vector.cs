using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Signal with multiple nodes inside of it (array)
/// </summary>
public class Vector : NamedSignal, ITopLevelNamedSignal
{
    private readonly int dimension;

    private readonly VectorNode[] vectorNodes;

    /// <summary>
    /// Create a vector given name, parent module, and dimension
    /// </summary>
    /// <param name="name">name of signal</param>
    /// <param name="parentModule">module to which this signal belongs</param>
    /// <param name="dimension">length of vector</param>
    public Vector(string name, IModule parentModule, int dimension)
    {
        if (dimension < 2)
            throw new ArgumentException("Dimension should be > 1");
        this.dimension = dimension;
        Name = name;
        ParentModule = parentModule;
        vectorNodes = Enumerable.Range(0, dimension).Select(i => new VectorNode(this, i)).ToArray();
    }

    /// <summary>
    /// Name of the signal
    /// </summary>
    public override string Name { get; }

    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public override IModule ParentModule { get; }

    /// <summary>
    /// How many nodes are part of this signal (1 for base version)
    /// </summary>
    public override DefiniteDimension Dimension => new(dimension);

    /// <inheritdoc/>
    public override string VhdlType => $"std_logic_vector({dimension-1} downto 0)";

    /// <inheritdoc/>
    public override IEnumerable<VectorNode> ToSingleNodeSignals => [.. vectorNodes];

    /// <inheritdoc/>
    public override INamedSignal? ParentSignal => null;

    /// <inheritdoc/>
    public override Vector TopLevelSignal => this;

    /// <inheritdoc/>
    public override IEnumerable<VectorNode> ChildSignals => [.. vectorNodes];
    
    /// <summary>
    /// Access individual node signals of vector. 
    /// These can be used as single-node signals
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public override VectorNode this[int index]
    {
        get
        {
            if (index < dimension && index >= 0)
                return vectorNodes[index];
            throw new Exception($"Index ({index}) must be less than dimension ({dimension}) and nonnegative");
        }
    }

    /// <inheritdoc/>
    public override INamedSignal this[Range range]
    {
        get
        {
            int start = range.Start.GetOffset(dimension);
            int end = range.End.GetOffset(dimension);
            if (start > end || start < 0 || end > dimension)
                throw new ArgumentOutOfRangeException(nameof(range), "Specified range doesn't work for this vector");
            return new VectorSlice(this, start, end);
        }
    }

    /// <inheritdoc/>
    public override bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        // If there's a named signal (with a parent), check that one--otherwise, get the first available
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
}