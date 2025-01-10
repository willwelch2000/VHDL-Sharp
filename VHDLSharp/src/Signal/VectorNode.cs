using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Node in a vector
/// </summary>
/// <param name="vector">The vector it's a part of</param>
/// <param name="node">The index in that vector</param>
public class VectorNode(Vector vector, int node) : SingleNodeSignal
{
    /// <inheritdoc/>
    public Vector Vector { get; private init; } = vector;

    /// <inheritdoc/>
    public int Node { get; private init; } = node;
    
    /// <inheritdoc/>
    public override string Name => $"{Vector.Name}[{Node}]";

    /// <inheritdoc/>
    public override Module ParentModule => Vector.ParentModule;

    /// <inheritdoc/>
    public override string ToSpice() => $"{Vector.Name}_{Node}";
}