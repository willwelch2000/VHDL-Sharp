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

    /// <summary>
    /// Generate an And with this logic tree and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public And<T> And(ILogicallyCombinable<T> other) => new(this, other);

    /// <summary>
    /// Generate an Or with this logic tree and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Or<T> Or(ILogicallyCombinable<T> other) => new(this, other);

    /// <summary>
    /// Generate a Not with this logic tree
    /// </summary>
    /// <returns></returns>
    public Not<T> Not() => new(this);

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <inheritdoc/>
    public abstract (string Value, TOut Additional) ToLogicString<TIn, TOut>(CustomLogicStringOptions<T, TIn, TOut> options, TIn additionalInput) where TOut : new();
}