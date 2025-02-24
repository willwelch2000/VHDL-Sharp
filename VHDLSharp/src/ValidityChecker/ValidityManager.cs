namespace VHDLSharp.Validation;

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
    private event EventHandler? EntityOrChildUpdated;

    /// <summary>
    /// Constructor given entity to track
    /// </summary>
    /// <param name="entity"></param>
    public ValidityManager(IValidityManagedEntity entity)
    {
        this.entity = entity;
        entity.Updated += EntityUpdated;
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
            child.Updated += ChildUpdated;
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
                child.Updated -= ChildUpdated;
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
    private void EntityUpdated(object? sender, EventArgs e)
    {
        // Check entity and all children, then invoke updated event so parent knows
        entity.CheckValidity();
        foreach (ValidityManager child in children.Keys)
        {
            child.CheckValidityFromParent();
        }
        EntityOrChildUpdated?.Invoke(this, EventArgs.Empty);
    }

    // Called when child ValidityManager is updated
    private void ChildUpdated(object? sender, EventArgs e)
    {
        // Check entity and all children but the one that called it, then invoke updated event so parent knows
        entity.CheckValidity();
        IEnumerable<ValidityManager> childrenToCheck = sender is ValidityManager senderAsChecker ? children.Keys.Except([senderAsChecker]) : children.Keys;
        foreach (ValidityManager child in childrenToCheck)
        {
            child.CheckValidityFromParent();
        }
        EntityOrChildUpdated?.Invoke(this, EventArgs.Empty);
    }

    // Called by parent ValidityManager
    private void CheckValidityFromParent()
    {
        // Check entity and all children, don't invoke updated event
        entity.CheckValidity();
        foreach (ValidityManager child in children.Keys)
        {
            child.CheckValidityFromParent();
        }
    }
}