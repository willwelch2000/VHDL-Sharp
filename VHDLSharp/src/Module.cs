using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace VHDLSharp;

public class Module
{
    public Module()
    {
        Nodes.CollectionChanged += NodesChanged;
        Behaviors.CollectionChanged += BehaviorsChanged;
    }

    public ObservableCollection<Node> Nodes { get; } = [];

    public ObservableCollection<IBehavior> Behaviors { get; } = [];

    public Dictionary<IDigitalEvent, List<IBehavior>> EventMapping { get; } = [];


    private void NodesChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is null)
            return;

        foreach (Node oldNode in args.OldItems)
        {
            if (!Nodes.Contains(oldNode) && !Behaviors.SelectMany(b => b.InvolvedNodes).Contains(oldNode))
            {
                throw new Exception("Cannot remove node");
            }
        }
    }

    private void BehaviorsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems is null)
            return;

        foreach (IBehavior newBehavior in args.NewItems)
        {
            foreach (Node node in newBehavior.InvolvedNodes)
            {
                if (!Nodes.Contains(node))
                {
                    Nodes.Add(node);
                }
            }
        }
    }
}
