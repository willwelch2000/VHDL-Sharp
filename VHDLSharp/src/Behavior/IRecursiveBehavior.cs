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
}