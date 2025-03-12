using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any signal that has a name and belongs to a <see cref="IModule"/>
/// </summary>
public interface INamedSignal : ISignal
{
    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public IModule ParentModule { get; }

    /// <summary>
    /// Name of the signal
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Type of signal as VHDL (e.g. std_logic)
    /// </summary>
    public string VhdlType { get; }

    /// <summary>
    /// Get signal declaration as VHDL
    /// </summary>
    /// <returns></returns>
    public string GetVhdlDeclaration();

    /// <summary>
    /// If this has a dimension > 1, convert to a list of named signals with dimension 1. 
    /// If it is dimension 1, then return itself
    /// </summary>
    public new IEnumerable<ISingleNodeNamedSignal> ToSingleNodeSignals { get; }

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;

    /// <summary>
    /// If this is the top level, it returns this. 
    /// Otherwise, it goes up in hierarchy as much as possible
    /// </summary>
    public new INamedSignal TopLevelSignal { get; }

    ISignal ISignal.TopLevelSignal => TopLevelSignal;

    /// <summary>
    /// If this is part of a larger group (e.g. vector node), get the parent signal (one layer up)
    /// </summary>
    public new INamedSignal? ParentSignal { get; }

    ISignal? ISignal.ParentSignal => ParentSignal;
}