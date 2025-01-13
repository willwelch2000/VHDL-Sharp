namespace VHDLSharp.LogicTree;

/// <summary>
/// Class that gives process for generating a logic string by providing a process for And, Or, Not, and Base
/// </summary>
/// <typeparam name="T">Thing of type <see cref="ILogicallyCombinable{T}"/> that is combined</typeparam>
/// <typeparam name="TIn">Additional input type of functions</typeparam>
/// <typeparam name="TOut">Additional return type of functions for use within the functions themselves</typeparam>
public class CustomLogicStringOptions<T, TIn, TOut> where T : ILogicallyCombinable<T> where TOut : new()
{
    /// <summary>
    /// Function for And to use
    /// Input is list of arguments to And
    /// </summary>
    public Func<IEnumerable<ILogicallyCombinable<T>>, TIn, (string Value, TOut Additional)> AndFunction { get; set; } = (expressions, input) => ("", new());

    /// <summary>
    /// Function for Or to use
    /// Input is list of arguments to Or
    /// </summary>
    public Func<IEnumerable<ILogicallyCombinable<T>>, TIn, (string Value, TOut Additional)> OrFunction { get; set; } = (expression, input) => ("", new());

    /// <summary>
    /// Function for Not to use
    /// Input is argument to Not
    /// </summary>
    public Func<ILogicallyCombinable<T>, TIn, (string Value, TOut Additional)> NotFunction { get; set; } = (expression, input) => ("", new());

    /// <summary>
    /// Function for base object to use
    /// By default, the <see cref="ILogicallyCombinable{T}"/> interface calls this function
    /// </summary>
    public Func<T, TIn, (string Value, TOut Additional)> BaseFunction { get; set; } = (thing, input) => ("", new());
}