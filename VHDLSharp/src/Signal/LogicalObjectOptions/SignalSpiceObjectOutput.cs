namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;

/// <summary>
/// Class used as return value for <see cref="CustomLogicObjectOptions{T, TIn, TOut}"/> when generating Spice
/// </summary>
internal class SignalSpiceObjectOutput
{
    /// <summary>
    /// Spice string output
    /// </summary>
    internal string SpiceString { get; set; } = "";

    /// <summary>
    /// How many nodes there are in this portion
    /// </summary>
    internal int Dimension { get; set; } = 0;

    /// <summary>
    /// Signal names of this portion
    /// </summary>
    internal string[] OutputSignalNames { get; set; } = [];
}