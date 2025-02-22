using VHDLSharp.Signals;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// Interface for anything that can be used as a port in a <see cref="IModule"/>
/// TODO might not need interface for this
/// </summary>
public interface IPort : IValidityManagedEntity
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