namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;

/// <summary>
/// Class used as return value for <see cref="CustomLogicObjectOptions{T, TIn, TOut}"/> when generating VHDL
/// </summary>
internal class SignalVhdlObjectOutput
{
    /// <summary>
    /// Spice string output
    /// </summary>
    internal string VhdlString { get; set; } = "";
}