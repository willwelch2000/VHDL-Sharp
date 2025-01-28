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
}