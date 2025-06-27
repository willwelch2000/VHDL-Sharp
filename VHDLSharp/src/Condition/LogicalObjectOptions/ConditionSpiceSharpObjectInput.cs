namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;
using SpiceSharp;

/// <summary>
/// Class used as input for <see cref="CustomLogicObjectOptions{T, TIn, TOut}"/> when generating Spice# <see cref="Circuit"/> object from a condition tree
/// </summary>
internal class ConditionSpiceSharpObjectInput
{
    /// <summary>
    /// Unique id that this portion can use.
    /// Should contain numbers and underscores
    /// </summary>
    internal string UniqueId { get; set; } = string.Empty;
}