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
            throw new Exception("And should have > 1 inputs");
        this.inputs = inputs;
    }

    /// <inheritdoc/>
    public override IEnumerable<ILogicallyCombinable<T>> Inputs => inputs;

    /// <inheritdoc/>
    public override IEnumerable<T> AllBaseObjects
    {
        get
        {
            foreach (var input in inputs)
                if (input is T inputAsT)
                    yield return inputAsT;
                else if (input is LogicTree<T> inputAsLogicTree)
                    foreach (var subObject in inputAsLogicTree.AllBaseObjects)
                        yield return subObject;
        }
    }

    /// <inheritdoc/>
    public override string ToLogicString() => string.Join(" and ", inputs.Select(i => $"{i.ToLogicString()}"));
}