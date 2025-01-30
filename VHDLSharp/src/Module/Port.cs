using VHDLSharp.Signals;

namespace VHDLSharp.Modules;

/// <summary>
/// Directions that a port can have with respect to a module
/// </summary>
public enum PortDirection
{
    /// <summary>
    /// Input to the module
    /// </summary>
    Input,
    /// <summary>
    /// Output from the module
    /// </summary>
    Output,
    /// <summary>
    /// Input/output
    /// </summary>
    Bidirectional
}

/// <summary>
/// A signal that is a port of a module
/// </summary>
public class Port : IHasParentModule
{
    private NamedSignal? signal;

    /// <summary>
    /// The signal object that this refers to
    /// </summary>
    public required NamedSignal Signal
    {
        get => signal ?? throw new Exception("Should be impossible");
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

    /// <inheritdoc/>
    public Module ParentModule => Signal.ParentModule;

    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Signal.Name;

    /// <summary>
    /// Get signal as VHDL
    /// </summary>
    /// <returns></returns>
    public string ToVhdl() => $"{Signal.Name}\t: {DirectionToVhdl(Direction)}\t{Signal.VhdlType}";

    private static string DirectionToVhdl(PortDirection direction) =>
        direction switch
        {
            PortDirection.Input => "in",
            PortDirection.Output => "out",
            PortDirection.Bidirectional => "inout",
            _ => "inout",
        };
}