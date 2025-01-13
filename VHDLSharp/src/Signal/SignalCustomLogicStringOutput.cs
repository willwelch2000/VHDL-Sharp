namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;

/// <summary>
/// Class used as additional return value for <see cref="CustomLogicStringOptions{T, TIn, TOut}"/>
/// </summary>
public class SignalCustomLogicStringOutput
{
    /// <summary>
    /// How many nodes there are in this portion
    /// </summary>
    public int Dimension { get; set; } = 0;

    /// <summary>
    /// Signal names of this portion
    /// </summary>
    public string[] OutputSignalNames { get; set; } = [];
}