namespace VHDLSharp;

/// <summary>
/// Signal with multiple nodes inside of it (array)
/// </summary>
public class Vector : ISignal
{
    private readonly int dimension;

    private readonly VectorNode[] vectorNodes;

    /// <summary>
    /// Create a vector given name, parent, and dimension
    /// </summary>
    /// <param name="name">name of signal</param>
    /// <param name="parent">module to which this signal belongs</param>
    /// <param name="dimension">length of vector</param>
    public Vector(string name, Module parent, int dimension)
    {
        if (dimension < 2)
            throw new ArgumentException("Dimension should be > 1");
        this.dimension = dimension;
        Name = name;
        Parent = parent;
        vectorNodes = Enumerable.Range(0, dimension).Select(i => new VectorNode(this, i)).ToArray();
    }

    /// <summary>
    /// Name of the signal
    /// </summary>
    public string Name { get; private init; }

    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public Module Parent { get; private init; }

    /// <summary>
    /// How many nodes are part of this signal (1 for base version)
    /// </summary>
    public int Dimension => dimension;

    /// <inheritdoc/>
    public string VhdlType => $"std_logic_vector({Dimension-1} downto 0)";

    /// <inheritdoc/>
    public string ToVhdl => $"signal {Name}\t: {VhdlType}";
    
    /// <summary>
    /// Access individual node signals of vector
    /// These can be used as single-node signals
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public VectorNode this[int index]
    {
        get
        {
            if (index < dimension && index >= 0)
                return vectorNodes[index];
            else
                throw new Exception($"Index ({index}) must be less than dimension ({dimension}) and nonnegative");
        }
    }

    /// <inheritdoc/>
    public bool CanCombine(ISignal other) =>
        Dimension == other.Dimension && Parent == other.Parent;

    /// <inheritdoc/>
    public string ToLogicString() => Name;

    /// <summary>
    /// Convert signal to signal expression
    /// </summary>
    /// <param name="vector"></param>
    public static implicit operator SignalExpression(Vector vector) => new(vector);
}