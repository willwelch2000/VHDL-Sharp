namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;

/// <summary>
/// Class used as input for <see cref="CustomLogicObjectOptions{T, TIn, TOut}"/> when generating Spice
/// </summary>
internal class SignalSpiceObjectInput
{
    /// <summary>
    /// Unique id that this portion can use. 
    /// Should contain numbers and underscores
    /// </summary>
    internal string UniqueId { get; set; } = string.Empty;
}