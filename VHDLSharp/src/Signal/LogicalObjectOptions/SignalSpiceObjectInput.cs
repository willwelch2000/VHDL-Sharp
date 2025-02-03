namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;

/// <summary>
/// Class used as input for <see cref="CustomLogicObjectOptions{T, TIn, TOut}"/> when generating Spice
/// </summary>
public class SignalSpiceObjectInput
{
    /// <summary>
    /// Unique id that this portion can use. 
    /// Should contain numbers and underscores
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;
}