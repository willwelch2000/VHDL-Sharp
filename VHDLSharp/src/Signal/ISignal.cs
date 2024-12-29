using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// Single-node and vector signals
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal, SignalLogicStringOptions>
{
    /// <summary>
    /// Name of the signal
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public Module Parent { get; }

    /// <summary>
    /// How many nodes are part of this signal (1 for base version)
    /// </summary>
    public DefiniteDimension Dimension { get; }

    /// <summary>
    /// Type of signal as VHDL
    /// </summary>
    public string VhdlType { get; }

    /// <summary>
    /// Get signal as VHDL
    /// </summary>
    /// <returns></returns>
    public string ToVhdl { get; }
}