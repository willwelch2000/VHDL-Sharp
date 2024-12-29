namespace VHDLSharp.LogicTree;

/// <summary>
/// Logical not of one input
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="V"></typeparam>
/// <param name="input"></param>
/// <exception cref="Exception"></exception>
public class Not<T, V>(ILogicallyCombinable<T, V> input) : LogicTree<T, V> where T : ILogicallyCombinable<T, V> where V : LogicStringOptions
{
    private readonly ILogicallyCombinable<T, V> input = input;

    /// <inheritdoc/>
    public override IEnumerable<ILogicallyCombinable<T, V>> Inputs => [input];

    /// <inheritdoc/>
    public override IEnumerable<T> BaseObjects => input.BaseObjects;

    /// <inheritdoc/>
    public override string ToLogicString(V options) => $"not ({input.ToLogicString(options)})";

    /// <inheritdoc/>
    public override string ToLogicString() => $"not ({input.ToLogicString()})";
}