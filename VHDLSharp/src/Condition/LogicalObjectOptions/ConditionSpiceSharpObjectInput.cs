namespace VHDLSharp.Conditions;

using VHDLSharp.LogicTree;
using SpiceSharp;
using VHDLSharp.Signals;

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

    /// <summary>
    /// Desired output name for this circuit
    /// </summary>
    internal required ISingleNodeNamedSignal OutputSignal { get; set; }
}