namespace VHDLSharp.LogicTree;

/// <summary>
/// Logic tree where the end result is type T
/// For example OR(AND(T1, T2), NOT(T3))
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class LogicTree<T> : ILogicallyCombinable<T> where T : ILogicallyCombinable<T>
{
    /// <summary>
    /// Direct inputs to this tree (non-recursive)
    /// </summary>
    public abstract IEnumerable<ILogicallyCombinable<T>> Inputs { get; }

    /// <summary>
    /// Get (recursively) all base objects used in the tree
    /// </summary>
    public abstract IEnumerable<T> BaseObjects { get; }

    /// <summary>
    /// Get first base object to use for accessing a representative example
    /// For example, to access the dimension of the logic tree
    /// </summary>
    public T? FirstBaseObject => BaseObjects.FirstOrDefault();

    /// <summary>
    /// Check ability to combine in boht directions
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool CanCombine(ILogicallyCombinable<T> other) => !BaseObjects.Any() || BaseObjects.All(o => o.CanCombine(other) && other.CanCombine(o));

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <inheritdoc/>
    public abstract TOut GenerateLogicalObject<TIn, TOut>(CustomLogicObjectOptions<T, TIn, TOut> options, TIn additionalInput) where TOut : new();

    /// <inheritdoc/>
    public bool CanCombine(IEnumerable<ILogicallyCombinable<T>> others)
    {
        T? first = FirstBaseObject;

        // If no base objects exist for this object, it's determined by the others
        // Test the first other with all the remaining others
        if (first is null)
            return !others.Any() || others.First().CanCombine(others.Skip(1));

        // Otherwise, test the first base object with the others
        return first.CanCombine(others);
    }
}