namespace VHDLSharp.LogicTree;

/// <summary>
/// Logical and between two or more inputs
/// </summary>
/// <typeparam name="T"></typeparam>
public class And<T> : LogicTree<T> where T : ILogicallyCombinable<T>
{
    private readonly ILogicallyCombinable<T>[] inputs;

    /// <summary>
    /// Generate new And
    /// </summary>
    /// <param name="inputs"></param>
    /// <exception cref="Exception"></exception>
    public And(params ILogicallyCombinable<T>[] inputs)
    {
        if (inputs.Length < 2)
            throw new Exception("AND should have > 1 inputs");
        if (!inputs[0].CanCombine(inputs[1..]))
            throw new Exception("Inputs to AND must be compatible");
        this.inputs = inputs;
    }

    /// <inheritdoc/>
    public override IEnumerable<ILogicallyCombinable<T>> Inputs => inputs;

    /// <inheritdoc/>
    public override IEnumerable<T> BaseObjects => inputs.SelectMany(i => i.BaseObjects);

    /// <inheritdoc/>
    public override string ToLogicString() => "(" + string.Join(" and ", inputs.Select(i => $"{i.ToLogicString()}")) + ")";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override TOut GenerateLogicalObject<TIn, TOut>(CustomLogicObjectOptions<T, TIn, TOut> options, TIn additionalInput) => options.AndFunction(inputs, additionalInput);

    /// <inheritdoc/>
    public override V PerformFunction<V>(Func<T, V> primary, Func<IEnumerable<V>, V> and, Func<IEnumerable<V>, V> or, Func<V, V> not) =>
        and(inputs.Select(i => i.PerformFunction(primary, and, or, not)));

    /// <inheritdoc/>
    public override bool Equals(ILogicallyCombinable<T>? other) => other is And<T> andOther &&
        Inputs.SequenceEqual(andOther.Inputs);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is And<T> andOther && Equals(andOther);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (var input in Inputs)
            hash.Add(input);
        hash.Add(ExpressionHashType(this));
        return hash.ToHashCode();
    }
}