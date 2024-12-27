namespace VHDLSharp.LogicTree;

/// <summary>
/// Interface for anything that can be used as the end type of a logic tree
/// </summary>
/// <typeparam name="T">For outside use, this should be the implementing type itself</typeparam>
public interface ILogicallyCombinable<T> where T : ILogicallyCombinable<T>
{
    /// <summary>
    /// Given another thing of type T, this is true if they are compatible for a logic tree
    /// Transitive property is assumed! (e.g. if A is compatible with B and B with C, then A must be compatible with C)
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool CanCombine(ILogicallyCombinable<T> other);

    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public string ToLogicString();

    /// <summary>
    /// Get all base objects
    /// If this is just a single thing, then it should return itself
    /// If this is a collection of T, then it should return that whole collection
    /// </summary>
    public IEnumerable<T> BaseObjects { get; }
}