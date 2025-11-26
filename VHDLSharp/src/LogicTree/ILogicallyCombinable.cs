namespace VHDLSharp.LogicTree;

/// <summary>
/// Interface for anything that can be used as the end type of a logic tree
/// </summary>
/// <typeparam name="T">For outside use, this should be the implementing type itself</typeparam>
public interface ILogicallyCombinable<T> : IEquatable<ILogicallyCombinable<T>> where T : ILogicallyCombinable<T>
{
    /// <summary>
    /// Given another thing of type T, this is true if they are compatible for a logic tree
    /// TODO I don't think this is true: Transitive property is assumed! (e.g. if A is compatible with B and B with C, then A must be compatible with C)
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool CanCombine(ILogicallyCombinable<T> other);

    /// <summary>
    /// Given other things of type T, this is true if they are all compatible for a logic tree.
    /// Should be overridden if there is a more efficient method
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public bool CanCombine(IEnumerable<ILogicallyCombinable<T>> others)
    {
        IEnumerable<ILogicallyCombinable<T>> all = [this, .. others];
        foreach (var thing1 in all)
            foreach (var thing2 in all.Except([thing1]))
                if (!thing1.CanCombine(thing2))
                    return false;

        return true;
    }

    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public string ToLogicString();

    /// <summary>
    /// Convert to string given options
    /// </summary>
    /// <returns></returns>
    public string ToLogicString(LogicStringOptions options);

    /// <summary>
    /// Convert to a custom logical object given custom options
    /// </summary>
    /// <typeparam name="TIn">Input type to functions</typeparam>
    /// <typeparam name="TOut">Output type</typeparam>
    /// <param name="options"></param>
    /// <param name="additionalInput"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public TOut GenerateLogicalObject<TIn, TOut>(CustomLogicObjectOptions<T, TIn, TOut> options, TIn additionalInput) where TOut : new()
    {
        if (this is T t)
            return options.BaseFunction(t, additionalInput);
        throw new Exception($"If this is not of type {typeof(T).Name}, it should override {nameof(GenerateLogicalObject)}");
    }

    /// <summary>
    /// Get all base objects
    /// If this is just a single thing, then it should return itself
    /// If this is a collection of T, then it should return that whole collection
    /// </summary>
    public IEnumerable<T> BaseObjects
    {
        get
        {
            if (this is T t)
                return [t];
            throw new Exception($"If this is not of type {typeof(T).Name}, it should override {nameof(BaseObjects)}");
        }
    }
    
    /// <summary>
    /// Generate an And with this and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public And<T> And(ILogicallyCombinable<T> other) => new(this, other);

    /// <summary>
    /// Generate an And with this and other <see cref="ILogicallyCombinable{T}"/> objects
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public And<T> And(IEnumerable<ILogicallyCombinable<T>> others) => new([this, .. others]);

    /// <summary>
    /// Generate an Or with this and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Or<T> Or(ILogicallyCombinable<T> other) => new(this, other);

    /// <summary>
    /// Generate an Or with this and other <see cref="ILogicallyCombinable{T}"/> objects
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public Or<T> Or(IEnumerable<ILogicallyCombinable<T>> others) => new([this, .. others]);

    /// <summary>
    /// Generate a Not with this
    /// </summary>
    /// <returns></returns>
    public Not<T> Not() => new(this);

    /// <summary>
    /// Perform a function on the combination, given a primary function and aggregation functions
    /// </summary>
    /// <typeparam name="V">Output type of functions</typeparam>
    /// <param name="primary">Function to be performed on things of type <typeparamref name="T"/></param>
    /// <param name="and">Aggregation function for AND</param>
    /// <param name="or">Aggregation function for OR</param>
    /// <param name="not">Aggregation function for NOT</param>
    /// <returns>Aggregated result</returns>
    /// <exception cref="Exception">If function is not overridden when it should be</exception>
    public V PerformFunction<V>(Func<T, V> primary, Func<IEnumerable<V>, V> and, Func<IEnumerable<V>, V> or, Func<V, V> not)
    {
        if (this is T t)
            return primary(t);
        throw new Exception($"If this is not of type {typeof(T).Name}, it should override {nameof(PerformFunction)}");
    }

    bool IEquatable<ILogicallyCombinable<T>>.Equals(ILogicallyCombinable<T>? other)
    {
        if (this is T t)
            return Equals(t, other);
        throw new Exception($"If this is not of type {typeof(T).Name}, it should override {nameof(Equals)}");
    }
}