namespace VHDLSharp.LogicTree;

/// <summary>
/// Class that gives process for generating a logic string by providing a process for And, Or, Not, and Base
/// </summary>
/// <typeparam name="T"></typeparam>
public class CustomLogicStringOptions<T> where T : ILogicallyCombinable<T>
{
    /// <summary>
    /// Function for And to use
    /// Input is list of arguments to And
    /// </summary>
    public Func<IEnumerable<ILogicallyCombinable<T>>, string> AndFunction { get; set; } = expression => "";

    /// <summary>
    /// Function for Or to use
    /// Input is list of arguments to Or
    /// </summary>
    public Func<IEnumerable<ILogicallyCombinable<T>>, string> OrFunction { get; set; } = expression => "";

    /// <summary>
    /// Function for Not to use
    /// Input is argument to Not
    /// </summary>
    public Func<ILogicallyCombinable<T>, string> NotFunction { get; set; } = expression => "";

    /// <summary>
    /// Function for base object to use
    /// By default, the <see cref="ILogicallyCombinable{T}"/> interface calls this function
    /// </summary>
    public Func<ILogicallyCombinable<T>, string> BaseFunction { get; set; } = expression => "";
}