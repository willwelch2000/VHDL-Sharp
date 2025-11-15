using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Dimensions;
using VHDLSharp.Signals;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Interface for behaviors that might have recursion, so that must be checked
/// </summary>
internal interface IRecursiveBehavior : IBehavior
{
    /// <summary>
    /// Get input signals of behavior, avoiding looking into anything in the <paramref name="behaviorsToIgnore"/> list.
    /// Avoids infinite recursion
    /// </summary>
    /// <param name="behaviorsToIgnore">List of behaviors to avoid looking into</param>
    /// <returns></returns>
    IEnumerable<IModuleSpecificSignal> GetInputModuleSignals(ISet<IBehavior> behaviorsToIgnore);

    /// <summary>
    /// Get dimension of behavior, avoiding looking into anything in the <paramref name="behaviorsToIgnore"/> list.
    /// It should act as if those behaviors aren't referenced or have no dimension specifications.
    /// Avoids infinite recursion (it normally looks into used behaviors to decide dimension)
    /// </summary>
    /// <param name="behaviorsToIgnore">List of behaviors to avoid looking into</param>
    /// <returns></returns>
    Dimension GetDimension(ISet<IBehavior> behaviorsToIgnore);

    IEnumerable<IBehavior> ChildBehaviors { get; }

    bool CheckRecursion(ISet<IRecursiveBehavior>? parentBehaviors = null)
    {
        if (parentBehaviors is not null && parentBehaviors.Contains(this))
            return true;
        HashSet<IRecursiveBehavior> childParentBehaviors = parentBehaviors is null ? [this] : [.. parentBehaviors, this];
        foreach (IRecursiveBehavior behavior in ChildBehaviors.OfType<IRecursiveBehavior>())
            if (behavior.CheckRecursion(childParentBehaviors))
                return true;
        return false;
    }
}