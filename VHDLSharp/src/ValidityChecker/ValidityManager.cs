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
/// Class to manage validity checking in a hierarchy of objects that can change
/// </summary>
public class ValidityManager
{
    // Entity this refers to--top of this tree
    private readonly IValidityManagedEntity entity;

    // Managers of tracked entities--int is count of how many times its been added
    private readonly Dictionary<ValidityManager, int> trackedEntityManagers = [];

    /// <summary>
    /// Event called when entity or tracked manager is updated
    /// </summary>
    private event EventHandler<ValidityManagerEventArgs>? ThisOrTrackedEntityUpdated;

    private Guid? mostRecentGuid = null;

    /// <summary>
    /// Constructor given entity to track
    /// </summary>
    /// <param name="entity"></param>
    public ValidityManager(IValidityManagedEntity entity)
    {
        this.entity = entity;
        entity.Updated += RespondToUpdateFromEntity;
    }

    /// <summary>
    /// Add entity for tracking.
    /// A change in the tracked entity is treated as a change here
    /// </summary>
    /// <param name="tracked"></param>
    public void AddTrackedEntity(IValidityManagedEntity tracked)
    {
        var manager = tracked.ValidityManager;
        if (trackedEntityManagers.ContainsKey(manager))
            trackedEntityManagers[manager] += 1;
        else
        {
            trackedEntityManagers[manager] = 1;
            manager.ThisOrTrackedEntityUpdated += RespondToUpdateFromTracked;
        }
    }

    /// <summary>
    /// Add entity for tracking if correct type.
    /// A change in the tracked entity is treated as a change here
    /// </summary>
    /// <param name="tracked"></param>
    public void AddTrackedObjectIfEntity(object tracked)
    {
        if (tracked is IValidityManagedEntity trackedEntity)
            AddTrackedEntity(trackedEntity);
    }

    /// <summary>
    /// Remove entity for tracking
    /// </summary>
    /// <param name="tracked"></param>
    public void RemoveTrackedEntity(IValidityManagedEntity tracked)
    {
        var manager = tracked.ValidityManager;
        if (trackedEntityManagers.TryGetValue(manager, out int count))
            if (count == 1)
            {
                trackedEntityManagers.Remove(manager);
                manager.ThisOrTrackedEntityUpdated -= RespondToUpdateFromTracked;
            }
            else
                trackedEntityManagers[manager] -= 1;
    }

    /// <summary>
    /// Remove entity for tracking if correct type
    /// </summary>
    /// <param name="tracked"></param>
    public void RemoveChildIfEntity(object tracked)
    {
        if (tracked is IValidityManagedEntity trackedEntity)
            RemoveTrackedEntity(trackedEntity);
    }

    // Called when entity is updated
    private void RespondToUpdateFromEntity(object? sender, EventArgs e)
    {
        // Make new GUID and store
        Guid guid = Guid.NewGuid();
        mostRecentGuid = guid;

        // Check entity, then invoke updated event with new GUID so parent knows
        entity.CheckValidity();
        ThisOrTrackedEntityUpdated?.Invoke(this, new(guid));
    }

    // Called when tracked entity is updated
    private void RespondToUpdateFromTracked(object? sender, ValidityManagerEventArgs e)
    {
        // Check if this is a new GUID before doing anything--if not, this is a repeat
        if (mostRecentGuid.Equals(e.Guid))
            return;

        // Check entity, then invoke updated event so parent knows
        entity.CheckValidity();
        ThisOrTrackedEntityUpdated?.Invoke(this, e);

        // Save GUID
        mostRecentGuid = e.Guid;
    }
}