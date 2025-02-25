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

    // Children managers--int is count of how many times its been added
    private readonly Dictionary<ValidityManager, int> children = [];

    /// <summary>
    /// Event called when entity or child manager is updated
    /// </summary>
    private event EventHandler<ValidityManagerEventArgs>? EntityOrChildUpdated;

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
    /// Add child entity for tracking.
    /// A change in the child is treated as a change here
    /// </summary>
    /// <param name="child"></param>
    public void AddChild(IValidityManagedEntity child)
    {
        var manager = child.ValidityManager;
        if (children.ContainsKey(manager))
            children[manager] += 1;
        else
        {
            children[manager] = 1;
            manager.EntityOrChildUpdated += RespondToUpdateFromChild;
        }
    }

    /// <summary>
    /// Add child entity for tracking if correct type.
    /// A change in the child is treated as a change here
    /// </summary>
    /// <param name="child"></param>
    public void AddChildIfEntity(object child)
    {
        if (child is IValidityManagedEntity entityChild)
            AddChild(entityChild);
    }

    /// <summary>
    /// Remove child entity for tracking
    /// </summary>
    /// <param name="child"></param>
    public void RemoveChild(IValidityManagedEntity child)
    {
        var manager = child.ValidityManager;
        if (children.TryGetValue(manager, out int count))
            if (count == 1)
            {
                children.Remove(manager);
                manager.EntityOrChildUpdated -= RespondToUpdateFromChild;
            }
            else
                children[manager] -= 1;
    }

    /// <summary>
    /// Remove child entity for tracking
    /// </summary>
    /// <param name="child"></param>
    public void RemoveChildIfEntity(object child)
    {
        if (child is IValidityManagedEntity entityChild)
            RemoveChild(entityChild);
    }

    // Called when entity is updated
    private void RespondToUpdateFromEntity(object? sender, EventArgs e)
    {
        // Make new GUID and store
        Guid guid = Guid.NewGuid();
        mostRecentGuid = guid;

        // Check entity, then invoke updated event with new GUID so parent knows
        entity.CheckValidity();
        EntityOrChildUpdated?.Invoke(this, new(guid));
    }

    // Called when child is updated
    private void RespondToUpdateFromChild(object? sender, ValidityManagerEventArgs e)
    {
        // Check if this is a new GUID before doing anything--if not, this is a repeat
        if (mostRecentGuid.Equals(e.Guid))
            return;

        // Check entity, then invoke updated event so parent knows
        entity.CheckValidity();
        EntityOrChildUpdated?.Invoke(this, e);

        // Save GUID
        mostRecentGuid = e.Guid;
    }
}