namespace VHDLSharp.Signals;

using VHDLSharp.LogicTree;
using SpiceSharp;
using SpiceSharp.Entities;

/// <summary>
/// Class used as return value for <see cref="CustomLogicObjectOptions{T, TIn, TOut}"/> when generating Spice# <see cref="Circuit"/> object
/// </summary>
public class SignalSpiceSharpObjectOutput
{
    /// <summary>
    /// How many nodes there are in this portion
    /// </summary>
    public int Dimension { get; set; } = 0;

    /// <summary>
    /// Signal names of this portion
    /// </summary>
    public string[] OutputSignalNames { get; set; } = [];

    /// <summary>
    /// Spice# entities added by the process
    /// </summary>
    public IEnumerable<IEntity> SpiceSharpEntities { get; set; } = [];
}