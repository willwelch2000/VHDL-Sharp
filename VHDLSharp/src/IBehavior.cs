namespace VHDLSharp;

public interface IBehavior
{
    public IEnumerable<Node> InvolvedNodes { get; }

    public event EventHandler NodesChanged;
}