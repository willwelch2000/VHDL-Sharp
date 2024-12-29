namespace VHDLSharp.LogicTree;

/// <summary>
/// Logic tree where the end result is type T
/// For example OR(AND(T1, T2), NOT(T3))
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="V"></typeparam>
public abstract class LogicTree<T, V> : ILogicallyCombinable<T, V> where T : ILogicallyCombinable<T, V> where V : LogicStringOptions
{
    /// <summary>
    /// Direct inputs to this tree (non-recursive)
    /// </summary>
    public abstract IEnumerable<ILogicallyCombinable<T, V>> Inputs { get; }

    /// <summary>
    /// Get (recursively) all base objects used in the tree
    /// </summary>
    public abstract IEnumerable<T> BaseObjects { get; }

    /// <summary>
    /// Get first base object to use for accessing a representative example
    /// For example, to access the dimension of the logic tree
    /// </summary>
    public T? FirstBaseObject => BaseObjects.FirstOrDefault();

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<T, V> other) => !BaseObjects.Any() || BaseObjects.First().CanCombine(other);

    /// <summary>
    /// Generate an And with this logic tree and another <see cref="ILogicallyCombinable{T, V}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public And<T, V> And(ILogicallyCombinable<T, V> other) => new(this, other);

    /// <summary>
    /// Generate an Or with this logic tree and another <see cref="ILogicallyCombinable{T, V}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Or<T, V> Or(ILogicallyCombinable<T, V> other) => new(this, other);

    /// <summary>
    /// Generate a Not with this logic tree
    /// </summary>
    /// <returns></returns>
    public Not<T, V> Not() => new(this);

    /// <inheritdoc/>
    public abstract string ToLogicString(V options);

    /// <inheritdoc/>
    public abstract string ToLogicString();
}