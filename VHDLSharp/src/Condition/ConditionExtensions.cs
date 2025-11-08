using VHDLSharp.LogicTree;

namespace VHDLSharp.Conditions;

/// <summary>
/// Extension methods for conditions
/// </summary>
public static class ConditionExtensions
{
    /// <summary>
    /// Logical And of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static And<ICondition> And(this ICondition condition, ILogicallyCombinable<ICondition> other) => new(condition, other);

    /// <summary>
    /// Logical Or of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Or<ICondition> Or(this ICondition condition, ILogicallyCombinable<ICondition> other) => new(condition, other);

    /// <summary>
    /// Logical Not of this condition
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    public static Not<ICondition> Not(this ICondition condition) => new(condition);

    /// <summary>
    /// Logical And of this condition and others
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static And<ICondition> And(this ICondition condition, IEnumerable<ILogicallyCombinable<ICondition>> others) => new([condition, .. others]);

    /// <summary>
    /// Logical Or of this condition and others
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static Or<ICondition> Or(this ICondition condition, IEnumerable<ILogicallyCombinable<ICondition>> others) => new([condition, .. others]);

    /// <summary>
    /// Logical And of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static And<IConstantCondition> And(this IConstantCondition condition, ILogicallyCombinable<IConstantCondition> other) => new(condition, other);

    /// <summary>
    /// Logical Or of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Or<IConstantCondition> Or(this IConstantCondition condition, ILogicallyCombinable<IConstantCondition> other) => new(condition, other);

    /// <summary>
    /// Logical Not of this condition
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    public static Not<IConstantCondition> Not(this IConstantCondition condition) => new(condition);

    /// <summary>
    /// Logical And of this condition and others
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static And<IConstantCondition> And(this IConstantCondition condition, IEnumerable<ILogicallyCombinable<IConstantCondition>> others) => new([condition, .. others]);

    /// <summary>
    /// Logical Or of this condition and others
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static Or<IConstantCondition> Or(this IConstantCondition condition, IEnumerable<ILogicallyCombinable<IConstantCondition>> others) => new([condition, .. others]);
}