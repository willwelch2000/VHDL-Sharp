namespace VHDLSharp;

/// <summary>
/// Interface for things that may have recursive nature and therefore must be checked
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMayBeRecursive<T> where T : IMayBeRecursive<T>
{
    /// <summary>
    /// Immediate children in recursive tree
    /// </summary>
    IEnumerable<IMayBeRecursive<T>> Children { get; }

    /// <summary>
    /// Checks object for recursion based on <see cref="Children"/>
    /// </summary>
    /// <param name="parents">Parents of object already found</param>
    /// <returns></returns>
    bool CheckRecursion(ISet<IMayBeRecursive<T>>? parents = null)
    {
        if (parents is not null && parents.Contains(this))
            return true;
        HashSet<IMayBeRecursive<T>> childParents = parents is null ? [this] : [.. parents, this];
        foreach (IMayBeRecursive<T> child in Children)
            if (child.CheckRecursion(childParents))
                return true;
        return false;
    }
}