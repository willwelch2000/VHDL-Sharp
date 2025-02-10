using VHDLSharp.Signals;

namespace VHDLSharp.Modules;

/// <summary>
/// A signal that is a port of a module
/// </summary>
public class Port : IPort
{
    private NamedSignal? signal;

    /// <summary>
    /// The signal object that this refers to
    /// </summary>
    public required NamedSignal Signal
    {
        get => signal ?? throw new("Should be impossible");
        set
        {
            signal = value;
            if (signal.ParentSignal is not null)
                throw new Exception("Port signal should be top-level signal");
        }
    }

    /// <summary>
    /// The direction that this port is with respect to the module
    /// </summary>
    public required PortDirection Direction { get; set; }

    /// <summary>
    /// Get signal as VHDL
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
}