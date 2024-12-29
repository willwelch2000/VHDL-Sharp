namespace VHDLSharp.LogicTree;

/// <summary>
/// Logical and between two or more inputs
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="V"></typeparam>
public class And<T, V> : LogicTree<T, V> where T : ILogicallyCombinable<T, V> where V : LogicStringOptions
{
    private readonly ILogicallyCombinable<T, V>[] inputs;

    /// <summary>
    /// Generate new And
    /// </summary>
    /// <param name="inputs"></param>
    /// <exception cref="Exception"></exception>
    public And(params ILogicallyCombinable<T, V>[] inputs)
    {
        if (inputs.Length < 2)
            throw new Exception("And should have > 1 inputs");
        this.inputs = inputs;
    }

    /// <inheritdoc/>
    public override IEnumerable<ILogicallyCombinable<T, V>> Inputs => inputs;

    /// <inheritdoc/>
    public override IEnumerable<T> BaseObjects => inputs.SelectMany(i => i.BaseObjects);

    /// <inheritdoc/>
    public override string ToLogicString(V options) => string.Join(" and ", inputs.Select(i => $"{i.ToLogicString(options)}"));

    /// <inheritdoc/>
    public override string ToLogicString() => string.Join(" and ", inputs.Select(i => $"{i.ToLogicString()}"));
}