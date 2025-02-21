namespace VHDLSharp.ValidityManager;

/// <summary>
/// Class to manage validity checking in a hierarchy of objects that can change
/// </summary>
public class ValidityManager
{
    // entity this refers to--top of this tree
    private readonly IValidityManageEntity entity;

    // children managers
    private readonly List<ValidityManager> children = [];

    // Event called when entity or child manager is updated
    private event EventHandler? Updated;

    /// <summary>
    /// Constructor given entity to track
    /// </summary>
    /// <param name="entity"></param>
    public ValidityManager(IValidityManageEntity entity)
    {
        this.entity = entity;
        entity.Updated += EntityUpdated;
    }

    /// <summary>
    /// Add child entity to track.
    /// A change in the child is treated as a change here
    /// </summary>
    /// <param name="child"></param>
    public void AddChild(IValidityManageEntity child)
    {
        children.Add(child.ValidityManager);
    }

    // Called when entity is updated
    private void EntityUpdated(object? sender, EventArgs e)
    {
        entity.CheckValidity();
        foreach (ValidityManager child in children)
        {
            child.CheckValidityFromParent();
        }
        Updated?.Invoke(this, EventArgs.Empty);
    }

    private void ChildUpdated(object? sender, EventArgs e)
    {
        IEnumerable<ValidityManager> childrenToCheck = sender is ValidityManager senderAsChecker ? children.Except([senderAsChecker]) : children;
        foreach (ValidityManager child in childrenToCheck)
        {
            child.CheckValidityFromParent();
        }
        Updated?.Invoke(this, EventArgs.Empty);
    }

    private void CheckValidityFromParent()
    {
        entity.CheckValidity();
        foreach (ValidityManager child in children)
        {
            child.CheckValidityFromParent();
        }
    }
}