namespace VHDLSharp.LogicTree;

/// <summary>
/// Logical not of one input
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="input"></param>
/// <exception cref="Exception"></exception>
public class Not<T>(ILogicallyCombinable<T> input) : LogicTree<T> where T : ILogicallyCombinable<T>
{
    private readonly ILogicallyCombinable<T> input = input;

    /// <inheritdoc/>
    public override IEnumerable<ILogicallyCombinable<T>> Inputs => [input];

    /// <inheritdoc/>
    public override IEnumerable<T> BaseObjects => input.BaseObjects;

    /// <inheritdoc/>
    public override string ToLogicString() => $"not ({input.ToLogicString()})";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override (string Value, TOut Additional) ToLogicString<TIn, TOut>(CustomLogicStringOptions<T, TIn, TOut> options, TIn additionalInput) => options.NotFunction(input, additionalInput);
}