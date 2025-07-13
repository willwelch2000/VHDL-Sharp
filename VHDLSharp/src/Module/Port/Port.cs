using VHDLSharp.Signals;

namespace VHDLSharp.Modules;

/// <summary>
/// A signal that is a port of a module
/// </summary>
/// <param name="signal"></param>
/// <param name="direction"></param>
public class Port(ITopLevelNamedSignal signal, PortDirection direction) : IPort
{
    /// <inheritdoc/>
    public ITopLevelNamedSignal Signal { get; } = signal;

    /// <inheritdoc/>
    public PortDirection Direction { get; } = direction;

    /// <inheritdoc/>
    public string GetVhdlDeclaration() => $"{Signal.Name}\t: {DirectionToVhdl(Direction)}\t{Signal.VhdlType}";

    private static string DirectionToVhdl(PortDirection direction) =>
        direction switch
        {
            PortDirection.Input => "in",
            PortDirection.Output => "out",
            // PortDirection.Bidirectional => "inout", TODO
            _ => "inout",
        };

    /// <inheritdoc/>
    public override string ToString() => Signal.ToString() ?? "";
}