using System.Collections.ObjectModel;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

public class TestNode : IValidityManagedEntity
{
    private readonly ValidityManager manager;
    private EventHandler? updated;
    private EventHandler? thisOrTrackedUpdated;
    public ObservableCollection<object> TrackedEntities { get; }

    public TestNode()
    {
        TrackedEntities = [];
        manager = new ValidityManager<object>(this, TrackedEntities);
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
    public void CheckValidity()
    {
        thisOrTrackedUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void InvokeUpdated() => updated?.Invoke(this, EventArgs.Empty);
}