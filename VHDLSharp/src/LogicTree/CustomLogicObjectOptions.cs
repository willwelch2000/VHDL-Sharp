namespace VHDLSharp.LogicTree;

/// <summary>
/// Class that gives process for generating a logic string by providing a process for And, Or, Not, and Base
/// </summary>
/// <typeparam name="T">Thing of type <see cref="ILogicallyCombinable{T}"/> that is combined</typeparam>
/// <typeparam name="TIn">Input type of functions, in addition to the thing(s) itself--should contain everything needed for sub-process</typeparam>
/// <typeparam name="TOut">Return type of functions--also used within the functions themselves--should contain everything needed from sub-process</typeparam>
public class CustomLogicObjectOptions<T, TIn, TOut> where T : ILogicallyCombinable<T> where TOut : new()
{
    /// <summary>
    /// Function for And to use
    /// Input is list of arguments to And, along with additional input of type TIn
    /// </summary>
    public Func<IEnumerable<ILogicallyCombinable<T>>, TIn, TOut> AndFunction { get; set; } = (expressions, input) => new();

    /// <summary>
    /// Function for Or to use
    /// Input is list of arguments to Or, along with additional input of type TIn
    /// </summary>
    public Func<IEnumerable<ILogicallyCombinable<T>>, TIn, TOut> OrFunction { get; set; } = (expression, input) => new();

    /// <summary>
    /// Function for Not to use
    /// Input is argument to Not, along with additional input of type TIn
    /// </summary>
    public Func<ILogicallyCombinable<T>, TIn, TOut> NotFunction { get; set; } = (expression, input) => new();

    /// <summary>
    /// Function for base object to use
    /// By default, the <see cref="ILogicallyCombinable{T}"/> interface calls this function
    /// Input is argument to the thing, along with additional input of type TIn
    /// </summary>
    public Func<T, TIn, TOut> BaseFunction { get; set; } = (thing, input) => new();
}