using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// A slice of a vector that includes some of the nodes, consecutively
/// </summary>
public class VectorSlice : NamedSignal
{
    /// <summary>
    /// Main constructor
    /// </summary>
    /// <param name="vector">Linked vector</param>
    /// <param name="startNode">Starting node, inclusive</param>
    /// <param name="endNode">Ending node, exclusive</param>
    internal VectorSlice(Vector vector, int startNode, int endNode)
    {
        Vector = vector;
        StartNode = startNode;
        EndNode = endNode;
    }

    /// <inheritdoc/>
    public override INamedSignal this[Range range]
    {
        get
        {
            int dim = Dimension.NonNullValue;
            return Vector[(range.Start.GetOffset(dim) + StartNode)..(range.End.GetOffset(dim) + StartNode)];
        }
    }

    /// <summary>The vector it's a part of</summary>
    public Vector Vector { get; }

    /// <summary>Starting node for the slice, inclusive</summary>
    public int StartNode { get; }

    /// <summary>Ending node for the slice, exclusive</summary>
    public int EndNode { get; }

    /// <inheritdoc/>
    public override string Name => $"{Vector.Name}[{StartNode}:{EndNode}]";

    /// <inheritdoc/>
    public override IModule ParentModule => Vector.ParentModule;

    /// <inheritdoc/>
    public override Vector? ParentSignal => Vector;

    /// <inheritdoc/>
    public override Vector TopLevelSignal => Vector;

    /// <inheritdoc/>
    public override string VhdlType => $"std_logic_vector({EndNode} downto {StartNode})";

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => new(EndNode - StartNode);

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> ChildSignals => ToSingleNodeSignals;

    /// <inheritdoc/>
    public override IEnumerable<ISingleNodeNamedSignal> ToSingleNodeSignals =>
        Enumerable.Range(StartNode, EndNode - StartNode).Select(i => Vector[i]);

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
    public override string GetVhdlName() => Vector.Name + $"({EndNode - 1} downto {StartNode})";

    /// <inheritdoc/>
    public override string ToLogicString() => Name;

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => Name;

    /// <inheritdoc/>
    public override bool IsPartOfPortMapping(PortMapping mapping, [MaybeNullWhen(false)] out INamedSignal equivalentSignal)
    {
        // Directly a port
        if (base.IsPartOfPortMapping(mapping, out equivalentSignal))
            return true;

        // Parent is a port
        IPort? port = mapping.Keys.FirstOrDefault(p => p.Signal == ParentSignal);
        if (port is null)
            return false;
        equivalentSignal = mapping[port][StartNode..EndNode];
        return true;
    }
}