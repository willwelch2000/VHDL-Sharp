using VHDLSharp.Signals;

namespace VHDLSharp.Modules;

/// <summary>
/// Interface for anything that can be used as a port in a <see cref="Module"/>
/// </summary>
public interface IPort
{
    /// <summary>
    /// The signal object that this refers to
    /// </summary>
    public INamedSignal Signal { get; }

    /// <summary>
    /// The direction that this port is with respect to the module
    /// </summary>
    public PortDirection Direction { get; }

    /// <summary>
    /// Get port as VHDL port declaration that goes in an entity declaration
    /// </summary>
    /// <returns></returns>
    public string GetVhdlDeclaration();
}