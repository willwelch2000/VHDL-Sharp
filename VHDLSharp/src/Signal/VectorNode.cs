using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Node in a vector
/// </summary>
public class VectorNode : SingleNodeNamedSignal
{
    /// <summary>
    /// Constructor given vector and node
    /// </summary>
    /// <param name="vector">The vector it's a part of</param>
    /// <param name="node">The index in that vector</param>
    internal VectorNode(Vector vector, int node)
    {
        Vector = vector;
        Node = node;
    }

    /// <summary>
    /// The vector it's a part of
    /// </summary>
    public Vector Vector { get; }

    /// <summary>
    /// The index in the vector
    /// </summary>
    public int Node { get; }
    
    /// <inheritdoc/>
    public override string Name => $"{Vector.Name}[{Node}]";

    /// <inheritdoc/>
    public override IModule ParentModule => Vector.ParentModule;

    /// <inheritdoc/>
    public override Vector? ParentSignal => Vector;

    /// <inheritdoc/>
    public override Vector TopLevelSignal => Vector;

    /// <inheritdoc/>
    public override string GetSpiceName() => $"{Vector.Name}_{Node}";

    /// <inheritdoc/>
    public override string GetVhdlDeclaration() => $"signal {Vector.Name}_{Node}\t: {VhdlType}";
}