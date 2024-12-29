namespace VHDLSharp.LogicTree;

/// <summary>
/// Interface for anything that can be used as the end type of a logic tree
/// </summary>
/// <typeparam name="T">For outside use, this should be the implementing type itself</typeparam>
/// <typeparam name="V">The type to use for logic string options</typeparam>
public interface ILogicallyCombinable<T, V> where T : ILogicallyCombinable<T, V> where V : LogicStringOptions
{
    /// <summary>
    /// Given another thing of type T, this is true if they are compatible for a logic tree
    /// Transitive property is assumed! (e.g. if A is compatible with B and B with C, then A must be compatible with C)
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool CanCombine(ILogicallyCombinable<T, V> other);

    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public string ToLogicString(V options);

    /// <summary>
    /// Convert to string using default options
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