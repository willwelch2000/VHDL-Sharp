using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace VHDLSharp.Validation;

/// <summary>
/// Event arguments for a validity manager event
/// </summary>
/// <param name="guid">Globally unique identifier used to differentiate events</param>
public class ValidityManagerEventArgs(Guid guid) : EventArgs
{
    /// <summary>
    /// Globally unique identifier used to differentiate events
    /// </summary>
    public Guid Guid => guid;
}

/// <summary>
/// Types of monitoring that can be performed by the manager
/// </summary>
public enum MonitorMode
{
    /// <summary>
    /// No alerts when main or tracked entity is changed
    /// </summary>
    Inactive,
    /// <summary>
    /// Raise alerts when main or tracked entity is changed
    /// </summary>
    AlertUpdates,
    /// <summary>
    /// Raise alerts when main or tracked entity is changed and throw exception when invalid
    /// </summary>
    AlertUpdatesAndThrowException
}

/// <summary>
/// Class to manage validity-checking for an object that implements <see cref="IValidityManagedEntity"/>
/// </summary>
public abstract class ValidityManager
{
    private readonly IValidityManagedEntity entity;

    /// <summary>
    /// True children of the main object that implement <see cref="IValidityManagedEntity"/>
    /// </summary>
    protected abstract IEnumerable<IValidityManagedEntity> ChildrenEntities { get; }

    // Managers of observed entities, including children and additional observed--int is count of how many times it's been added
    private readonly Dictionary<ValidityManager, int> observedEntityManagers = [];

    private Guid? mostRecentGuid = null;

    /// <summary>
    /// Event called when entity or observed manager is updated, only if <see cref="MonitorMode"/> is true.
    /// Not called if <see cref="MonitorMode"/> is <see cref="MonitorMode.Inactive"/>
    /// </summary>
    public event EventHandler<ValidityManagerEventArgs>? ChangeDetectedInMainOrTrackedEntity;

    /// <summary>
    /// Constructor given main entity, children, and entities to observe.
    /// Child entities are any that must be valid for the main entity to be considered valid. They also trigger an update here when <see cref="MonitorMode"/> is activated. 
    /// Observed entities are those that trigger an update here when <see cref="MonitorMode"/> is activated but are not true children of the main entity. 
    /// In other words, an invalid observed entity does not necessarily imply that the main entity is invalid, but an invalid child does. 
    /// </summary>
    /// <param name="entity">Main entity to follow</param>
    protected ValidityManager(IValidityManagedEntity entity)
    {
        this.entity = entity;
    }

    /// <summary>
    /// Monitoring mode that controls if changes raise alerts on <see cref="ChangeDetectedInMainOrTrackedEntity"/> and/or throw exceptions if invalid
    /// </summary>
    public static MonitorMode MonitorMode { get; set; } = MonitorMode.Inactive;

    /// <summary>
    /// Checks if this entity, and its recursive children, are valid
    /// </summary>
    /// <returns>True if valid, false if not</returns>
    public bool IsValid() => entity.CheckTopLevelValidity(out _) && ChildrenEntities.All(c => c.ValidityManager.IsValid());

    /// <summary>
    /// If this entity is invalid, this provides all the issues it has
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Issue> Issues()
    {
        if (!entity.CheckTopLevelValidity(out Exception? exception))
        {
            yield return new()
            {
                TopLevelEntity = entity,
                Exception = exception
            };
        }

        foreach (Issue issue in ChildrenEntities.SelectMany(c => c.ValidityManager.Issues()))
        {
            issue.FaultChain.AddFirst(issue.TopLevelEntity);
            issue.TopLevelEntity = entity;
            yield return issue;
        }
    }

    /// <summary>
    /// Follow a collection of observed entities
    /// </summary>
    /// <param name="observedEntityCollection">collection of entities to be observed</param>
    protected void FollowObservedEntityCollection<T>(ObservableCollection<T> observedEntityCollection) where T : notnull
    {
        entity.Updated += RespondToUpdateFromEntity;
        observedEntityCollection.CollectionChanged += ObservedEntitiesCollectionChanged;
        foreach (object observedEntity in observedEntityCollection)
            AddObservedObjectIfEntity(observedEntity);
    }

    private void ObservedEntitiesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                    foreach (object newItem in e.NewItems)
                        AddObservedObjectIfEntity(newItem);
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                    foreach (object oldItem in e.OldItems)
                        RemoveObservedObjectIfEntity(oldItem);
                break;

            case NotifyCollectionChangedAction.Reset:
                foreach (ValidityManager manager in observedEntityManagers.Keys)
                    manager.ChangeDetectedInMainOrTrackedEntity -= RespondToUpdateFromObserved;
                observedEntityManagers.Clear();
                break;

            case NotifyCollectionChangedAction.Move:
                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems is not null)
                    foreach (object newItem in e.NewItems)
                        AddObservedObjectIfEntity(newItem);
                if (e.OldItems is not null)
                    foreach (object oldItem in e.OldItems)
                        RemoveObservedObjectIfEntity(oldItem);
                break;
        }
    }

    /// <summary>
    /// Add entity for observing.
    /// A change in the observed entity is treated as a change here
    /// </summary>
    /// <param name="observed"></param>
    private void AddObservededEntity(IValidityManagedEntity observed)
    {
        var manager = observed.ValidityManager;
        if (observedEntityManagers.ContainsKey(manager))
            observedEntityManagers[manager] += 1;
        else
        {
            observedEntityManagers[manager] = 1;
            manager.ChangeDetectedInMainOrTrackedEntity += RespondToUpdateFromObserved;
        }
    }

    /// <summary>
    /// Add entity for observing if correct type.
    /// A change in the observed entity is treated as a change here
    /// </summary>
    /// <param name="observed"></param>
    private void AddObservedObjectIfEntity(object observed)
    {
        if (observed is IValidityManagedEntity observedEntity)
            AddObservededEntity(observedEntity);
    }

    /// <summary>
    /// Remove entity for observing
    /// </summary>
    /// <param name="observed"></param>
    private void RemoveObservedEntity(IValidityManagedEntity observed)
    {
        var manager = observed.ValidityManager;
        if (observedEntityManagers.TryGetValue(manager, out int count))
            if (count == 1)
            {
                observedEntityManagers.Remove(manager);
                manager.ChangeDetectedInMainOrTrackedEntity -= RespondToUpdateFromObserved;
            }
            else
                observedEntityManagers[manager] -= 1;
    }

    /// <summary>
    /// Remove entity for observing if correct type
    /// </summary>
    /// <param name="observed"></param>
    private void RemoveObservedObjectIfEntity(object observed)
    {
        if (observed is IValidityManagedEntity observedEntity)
            RemoveObservedEntity(observedEntity);
    }

    // Called when entity is updated
    // If this change is made, the NamedSignals caching in Module must change
    private void RespondToUpdateFromEntity(object? sender, EventArgs e)
    {
        // Don't do anything if monitoring is inactive
        if (MonitorMode == MonitorMode.Inactive)
            return;

        // Make new GUID and store
        Guid guid = Guid.NewGuid();
        mostRecentGuid = guid;

        // If throwing exceptions, check entity
        if (MonitorMode == MonitorMode.AlertUpdatesAndThrowException && !entity.CheckTopLevelValidity(out Exception? exception))
            throw exception;
        // Invoke change detected event with new GUID so parent knows
        ChangeDetectedInMainOrTrackedEntity?.Invoke(this, new(guid));
    }

    // Called when observed entity is updated
    private void RespondToUpdateFromObserved(object? sender, ValidityManagerEventArgs e)
    {
        // Don't do anything if monitoring is inactive--TODO might can remove
        if (MonitorMode == MonitorMode.Inactive)
            return;

        // Check if this is a new GUID before doing continuing--if not, this is a repeat
        if (mostRecentGuid.Equals(e.Guid))
            return;

        // Save GUID
        mostRecentGuid = e.Guid;

        // If throwing exceptions, check entity
        if (MonitorMode == MonitorMode.AlertUpdatesAndThrowException && !entity.CheckTopLevelValidity(out Exception? exception))
            throw exception;
        // Invoke change detected event so parent knows
        ChangeDetectedInMainOrTrackedEntity?.Invoke(this, e);
    }
}

/// <summary>
/// Class to manage validity-checking for an object that implements <see cref="IValidityManagedEntity"/>
/// </summary>
public class ValidityManager<T> : ValidityManager where T : notnull
{
    private readonly ObservableCollection<T> childrenEntitiesAsT;

    /// <inheritdoc/>
    protected override IEnumerable<IValidityManagedEntity> ChildrenEntities => childrenEntitiesAsT.Where(c => c is IValidityManagedEntity).Select(c => (IValidityManagedEntity)c);

    /// <summary>
    /// Constructor given main entity, children, and entities to observe.
    /// Child entities are any that must be valid for the main entity to be considered valid. They also trigger an update here when CheckAfterUpdate is activated. 
    /// Observed entities are those that trigger an update here when CheckAfterUpdate is activated but are not true children of the main entity. 
    /// In other words, an invalid observed entity does not necessarily imply that the main entity is invalid, but an invalid child does. 
    /// </summary>
    /// <param name="entity">Main entity to follow</param>
    /// <param name="childrenEntities">List of children to include in validity-checking. Children also trigger updates here when CheckAfterUpdate is activated</param>
    /// <param name="additionalObservedEntities">List of additional entities that invoke recheck of validity when CheckAfterUpdate is activated</param>
    public ValidityManager(IValidityManagedEntity entity, ObservableCollection<T> childrenEntities, ObservableCollection<T>? additionalObservedEntities = null) : base(entity)
    {
        childrenEntitiesAsT = childrenEntities;
        FollowObservedEntityCollection(childrenEntities);
        if (additionalObservedEntities is not null)
            FollowObservedEntityCollection(additionalObservedEntities);
    }
}