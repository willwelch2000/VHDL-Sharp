namespace VHDLSharp.LogicTree;

/// <summary>
/// Logical or between two or more inputs
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="V"></typeparam>
public class Or<T, V> : LogicTree<T, V> where T : ILogicallyCombinable<T, V> where V : LogicStringOptions
{
    private readonly ILogicallyCombinable<T, V>[] inputs;

    /// <summary>
    /// Generate new Or
    /// </summary>
    /// <param name="inputs"></param>
    /// <exception cref="Exception"></exception>
    public Or(params ILogicallyCombinable<T, V>[] inputs)
    {
        if (inputs.Length < 2)
            throw new Exception("Or should have > 1 inputs");
        this.inputs = inputs;
    }

    /// <inheritdoc/>
    public override IEnumerable<ILogicallyCombinable<T, V>> Inputs => inputs;

    /// <inheritdoc/>
    public override IEnumerable<T> BaseObjects => inputs.SelectMany(i => i.BaseObjects);

    /// <inheritdoc/>
    public override string ToLogicString(V options) => string.Join(" or ", inputs.Select(i => $"{i.ToLogicString(options)}"));

    /// <inheritdoc/>
    public override string ToLogicString() => string.Join(" or ", inputs.Select(i => $"{i.ToLogicString()}"));
}