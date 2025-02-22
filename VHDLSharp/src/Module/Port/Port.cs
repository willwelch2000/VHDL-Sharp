using VHDLSharp.Signals;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// A signal that is a port of a module
/// </summary>
public class Port : IPort
{
    private readonly ValidityManager validityManager;

    /// <summary>
    /// Constructor given signal and direction
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="direction"></param>
    public Port(INamedSignal signal, PortDirection direction)
    {
        Signal = signal;
        Direction = direction;
        validityManager = new(this);
    }

    ValidityManager IValidityManagedEntity.ValidityManager => validityManager;

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