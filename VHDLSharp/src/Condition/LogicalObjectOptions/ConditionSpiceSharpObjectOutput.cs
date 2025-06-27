namespace VHDLSharp.Conditions;

using VHDLSharp.LogicTree;
using SpiceSharp;
using SpiceSharp.Entities;

/// <summary>
/// Class used as return value for <see cref="CustomLogicObjectOptions{T, TIn, TOut}"/> when generating Spice# <see cref="Circuit"/> object from a condition tree
/// </summary>
internal class ConditionSpiceSharpObjectOutput
{
    /// <summary>
    /// Spice# entities added by the process
    /// </summary>
    internal IEnumerable<IEntity> SpiceSharpEntities { get; set; } = [];
}