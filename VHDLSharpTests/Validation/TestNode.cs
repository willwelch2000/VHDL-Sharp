using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

public class TestNode : IValidityManagedEntity
{
    private readonly ValidityManager manager;
    private EventHandler? updated;
    private EventHandler? thisOrTrackedUpdated;
    public ObservableCollection<object> ChildrenEntities { get; }
    public ObservableCollection<object> AdditionalTrackedEntities { get; }

    public TestNode()
    {
        ChildrenEntities = [];
        AdditionalTrackedEntities = [];
        manager = new ValidityManager<object>(this, ChildrenEntities, AdditionalTrackedEntities);
        manager.ChangeDetectedInMainOrTrackedEntity += (s, e) => thisOrTrackedUpdated?.Invoke(this, e);
    }

    public ValidityManager ValidityManager => manager;
    public event EventHandler? Updated
    {
        add => updated += value;
        remove => updated -= value;
    }
    public event EventHandler? ThisOrTrackedEntityUpdated
    {
        add => thisOrTrackedUpdated += value;
        remove => thisOrTrackedUpdated -= value;
    }

    public void InvokeUpdated() => updated?.Invoke(this, EventArgs.Empty);
}