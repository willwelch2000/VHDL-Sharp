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
    /// Event called when entity or observed manager is updated
    /// </summary>
    public event EventHandler<ValidityManagerEventArgs>? ThisOrObservedEntityUpdated;

    // private IEnumerable<IValidityManagedEntity> ChildrenAsEntities => ChildrenEntities.Where(c => c is IValidityManagedEntity).Select(c => (IValidityManagedEntity)c);

    /// <summary>
    /// Constructor given main entity, children, and entities to observe.
    /// Child entities are any that must be valid for the main entity to be considered valid. They also trigger an update here when <see cref="CheckAfterUpdate"/> is activated. 
    /// Observed entities are those that trigger an update here when <see cref="CheckAfterUpdate"/> is activated but are not true children of the main entity. 
    /// In other words, an invalid observed entity does not necessarily imply that the main entity is invalid, but an invalid child does. 
    /// </summary>
    /// <param name="entity">Main entity to follow</param>
    protected ValidityManager(IValidityManagedEntity entity)
    {
        this.entity = entity;
    }

    /// <summary>
    /// If true, throws an error after an invalid modification is made
    /// </summary>
    public static bool CheckAfterUpdate { get; set; } = false;

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
        if (!entity.CheckTopLevelValidity(out string? explanation))
        {
            yield return new()
            {
                TopLevelEntity = entity,
                Explanation = explanation
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
                    manager.ThisOrObservedEntityUpdated -= RespondToUpdateFromObserved;
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
            manager.ThisOrObservedEntityUpdated += RespondToUpdateFromObserved;
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
                manager.ThisOrObservedEntityUpdated -= RespondToUpdateFromObserved;
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
    // TODO might should change so that no updates happen if bool is false
    // If this change is made, the NamedSignals caching in Module must change
    private void RespondToUpdateFromEntity(object? sender, EventArgs e)
    {
        // Make new GUID and store
        Guid guid = Guid.NewGuid();
        mostRecentGuid = guid;

        // Check entity, then invoke updated event with new GUID so parent knows
        if (CheckAfterUpdate && !entity.CheckTopLevelValidity(out string? explanation))
            throw new Exception(explanation);
        ThisOrObservedEntityUpdated?.Invoke(this, new(guid));
    }

    // Called when observed entity is updated
    private void RespondToUpdateFromObserved(object? sender, ValidityManagerEventArgs e)
    {
        // Check if this is a new GUID before doing anything--if not, this is a repeat
        if (mostRecentGuid.Equals(e.Guid))
            return;

        // Save GUID
        mostRecentGuid = e.Guid;

        // Check entity, then invoke updated event so parent knows
        if (CheckAfterUpdate && !entity.CheckTopLevelValidity(out string? explanation))
            throw new Exception(explanation);
        ThisOrObservedEntityUpdated?.Invoke(this, e);
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
        this.childrenEntitiesAsT = childrenEntities;
        FollowObservedEntityCollection(childrenEntities);
        if (additionalObservedEntities is not null)
            FollowObservedEntityCollection(additionalObservedEntities);
    }
}