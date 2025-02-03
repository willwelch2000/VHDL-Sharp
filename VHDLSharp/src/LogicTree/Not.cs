namespace VHDLSharp.LogicTree;

/// <summary>
/// Logical not of one input
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="input"></param>
/// <exception cref="Exception"></exception>
public class Not<T>(ILogicallyCombinable<T> input) : LogicTree<T> where T : ILogicallyCombinable<T>
{
    /// <summary>
    /// Input to not function
    /// </summary>
    public ILogicallyCombinable<T> Input { get; } = input;

    /// <inheritdoc/>
    public override IEnumerable<ILogicallyCombinable<T>> Inputs => [Input];

    /// <inheritdoc/>
    public override IEnumerable<T> BaseObjects => Input.BaseObjects;

    /// <inheritdoc/>
    public override string ToLogicString() => $"(not ({Input.ToLogicString()}))";

    /// <inheritdoc/>
    public override string ToLogicString(LogicStringOptions options) => ToLogicString();

    /// <inheritdoc/>
    public override TOut GenerateLogicalObject<TIn, TOut>(CustomLogicObjectOptions<T, TIn, TOut> options, TIn additionalInput) => options.NotFunction(Input, additionalInput);
}