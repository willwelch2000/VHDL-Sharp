namespace VHDLSharp.Validation;

/// <summary>
/// Class to manage validity checking in a hierarchy of objects that can change
/// </summary>
public class ValidityManager
{
    // Entity this refers to--top of this tree
    private readonly IValidityManagedEntity entity;

    // Children managers
    private readonly List<ValidityManager> children = [];

    // Event called when entity or child manager is updated
    private event EventHandler? Updated;

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
        children.Add(child.ValidityManager);
        child.Updated += ChildUpdated;
    }

    /// <summary>
    /// Remove child entity for tracking
    /// </summary>
    /// <param name="child"></param>
    public void RemoveChild(IValidityManagedEntity child)
    {
        children.Remove(child.ValidityManager);
        child.Updated -= ChildUpdated;
    }

    // Called when entity is updated
    private void EntityUpdated(object? sender, EventArgs e)
    {
        // Check entity and all children, then invoke updated event so parent knows
        entity.CheckValidity();
        foreach (ValidityManager child in children)
        {
            child.CheckValidityFromParent();
        }
        Updated?.Invoke(this, EventArgs.Empty);
    }

    // Called when child ValidityManager is updated
    private void ChildUpdated(object? sender, EventArgs e)
    {
        // Check entity and all children but the one that called it, then invoke updated event so parent knows
        entity.CheckValidity();
        IEnumerable<ValidityManager> childrenToCheck = sender is ValidityManager senderAsChecker ? children.Except([senderAsChecker]) : children;
        foreach (ValidityManager child in childrenToCheck)
        {
            child.CheckValidityFromParent();
        }
        Updated?.Invoke(this, EventArgs.Empty);
    }

    // Called by parent ValidityManager
    private void CheckValidityFromParent()
    {
        // Check entity and all children, don't invoke updated event
        entity.CheckValidity();
        foreach (ValidityManager child in children)
        {
            child.CheckValidityFromParent();
        }
    }
}