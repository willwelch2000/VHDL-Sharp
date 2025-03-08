using VHDLSharp.Signals;

namespace VHDLSharp.Modules;

/// <summary>
/// A signal that is a port of a module
/// </summary>
/// <param name="signal"></param>
/// <param name="direction"></param>
public class Port(INamedSignal signal, PortDirection direction) : IPort
{
    /// <summary>
    /// The signal object that this refers to
    /// </summary>
    public INamedSignal Signal { get; } = signal;

    /// <summary>
    /// The direction that this port is with respect to the module
    /// </summary>
    public PortDirection Direction { get; } = direction;

    /// <summary>
    /// Get port as VHDL port declaration that goes in an entity declaration
    /// </summary>
    /// <returns></returns>
    public string GetVhdlDeclaration() => $"{Signal.Name}\t: {DirectionToVhdl(Direction)}\t{Signal.VhdlType}";

    private static string DirectionToVhdl(PortDirection direction) =>
        direction switch
        {
            PortDirection.Input => "in",
            PortDirection.Output => "out",
            PortDirection.Bidirectional => "inout",
            _ => "inout",
        };

    /// <inheritdoc/>
    public override string ToString() => Signal.ToString() ?? "";
}