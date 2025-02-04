using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Node in a vector
/// </summary>
/// <param name="vector">The vector it's a part of</param>
/// <param name="node">The index in that vector</param>
public class VectorNode(Vector vector, int node) : SingleNodeNamedSignal
{
    /// <summary>
    /// The vector it's a part of
    /// </summary>
    public Vector Vector { get; } = vector;

    /// <summary>
    /// The index in the vector
    /// </summary>
    public int Node { get; } = node;
    
    /// <inheritdoc/>
    public override string Name => $"{Vector.Name}[{Node}]";

    /// <inheritdoc/>
    public override Module ParentModule => Vector.ParentModule;

    /// <inheritdoc/>
    public override Vector? ParentSignal => Vector;

    /// <inheritdoc/>
    public override Vector TopLevelSignal => Vector;

    /// <inheritdoc/>
    public override string ToSpice() => $"{Vector.Name}_{Node}";

    /// <inheritdoc/>
    public override string ToVhdl() => $"signal {Vector.Name}_{Node}\t: {VhdlType}";
}