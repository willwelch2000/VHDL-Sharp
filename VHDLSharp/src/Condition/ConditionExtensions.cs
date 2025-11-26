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

    // Functions involving combinations of ICondition and IConstantCondition
    // Not the most elegant functionality, but it's probably good enough

    /// <summary>
    /// Logical And of this (constant) condition and another (not necessarily constant) condition
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static And<ICondition> And(this ILogicallyCombinable<IConstantCondition> condition, ILogicallyCombinable<ICondition> other) => new(condition.ToBasicCondition(), other);

    /// <summary>
    /// Logical Or of this (constant) condition and another (not necessarily constant) condition
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Or<ICondition> Or(this ILogicallyCombinable<IConstantCondition> condition, ILogicallyCombinable<ICondition> other) => new(condition.ToBasicCondition(), other);

    /// <summary>
    /// Logical And of this (constant) condition and other (not necessarily constant) conditions
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static And<ICondition> And(this ILogicallyCombinable<IConstantCondition> condition, IEnumerable<ILogicallyCombinable<ICondition>> others) => new([condition.ToBasicCondition(), .. others]);
    
    /// <summary>
    /// Logical Or of this (constant) condition and other (not necessarily constant) conditions
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="others"></param>
    /// <returns></returns>
    public static Or<ICondition> Or(this ILogicallyCombinable<IConstantCondition> condition, IEnumerable<ILogicallyCombinable<ICondition>> others) => new([condition.ToBasicCondition(), .. others]);

    /// <summary>
    /// Logical And of this (not necessarily constant) condition and another (constant) condition
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static And<ICondition> And(this ILogicallyCombinable<ICondition> condition, ILogicallyCombinable<IConstantCondition> other) => other.And(condition);

    /// <summary>
    /// Logical Or of this (not necessarily constant) condition and another (constant) condition
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Or<ICondition> Or(this ILogicallyCombinable<ICondition> condition, ILogicallyCombinable<IConstantCondition> other) => other.Or(condition);

    /// <summary>
    /// Convert from a constant-condition combo to a basic condition combo
    /// </summary>
    /// <param name="constantConditionCombo"></param>
    /// <returns></returns>
    public static ILogicallyCombinable<ICondition> ToBasicCondition(this ILogicallyCombinable<IConstantCondition> constantConditionCombo)
    {
        ICondition Primary(IConstantCondition condition) => condition;
        And<ICondition> And(IEnumerable<ILogicallyCombinable<ICondition>> inputs) => new([.. inputs]);
        Or<ICondition> Or(IEnumerable<ILogicallyCombinable<ICondition>> inputs) => new([.. inputs]);
        Not<ICondition> Not(ILogicallyCombinable<ICondition> input) => new(input);

        return constantConditionCombo.PerformFunction<ILogicallyCombinable<ICondition>>(Primary, And, Or, Not);
    }

    // Following functions exist to fix function priority issues

    /// <summary>
    /// Logical And of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static And<ICondition> And(this IConstantCondition condition, ILogicallyCombinable<ICondition> other) => new(condition, other);

    /// <summary>
    /// Logical Or of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Or<ICondition> Or(this IConstantCondition condition, ILogicallyCombinable<ICondition> other) => new(condition, other);

    /// <summary>
    /// Logical And of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static And<IConstantCondition> And(this IConstantCondition condition, IConstantCondition other) => new(condition, other);

    /// <summary>
    /// Logical Or of this condition and another
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Or<IConstantCondition> Or(this IConstantCondition condition, IConstantCondition other) => new(condition, other);
}