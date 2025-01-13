namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;

/// <summary>
/// Class used as additional input for <see cref="CustomLogicStringOptions{T, TIn, TOut}"/>
/// </summary>
public class SignalCustomLogicStringInput
{
    /// <summary>
    /// Unique id that this portion can use
    /// Should contain numbers and underscores
    /// </summary>
    public string UniqueId { get; set; } = string.Empty;
}