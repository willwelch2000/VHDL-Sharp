namespace VHDLSharp.Signals;

public class DerivedSignalNode : ISingleNodeSignal, IEquatable<DerivedSignalNode>
{
    /// <summary>
    /// Constructor given parent signal and node index
    /// </summary>
    /// <param name="derivedSignal">Parent derived signal</param>
    /// <param name="node">Index in the parent</param>
    public DerivedSignalNode(IDerivedSignal derivedSignal, int node)
    {
        DerivedSignal = derivedSignal;
        Node = node;
    }

    /// <summary>Parent <see cref="IDerivedSignal"/> that this belongs to</summary>
    public IDerivedSignal DerivedSignal { get; }

    /// <summary>The index in the <see cref="DerivedSignal"/></summary>
    public int Node { get; }

    /// <inheritdoc/>
    public bool Equals(DerivedSignalNode? other) =>
        other is not null && other.DerivedSignal == DerivedSignal && other.Node == Node;
}