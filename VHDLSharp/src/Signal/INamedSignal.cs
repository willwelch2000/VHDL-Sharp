using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Conditions;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any signal that has a name and belongs to a <see cref="IModule"/>
/// </summary>
public interface INamedSignal : IModuleSpecificSignal
{
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
    /// If this is a single-node signal, then return itself
    /// TODO should this be made into a list because it is assumed to be ordered consistently?
    /// </summary>
    public new IEnumerable<ISingleNodeNamedSignal> ToSingleNodeSignals { get; }

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;
    IEnumerable<ISingleNodeModuleSpecificSignal> IModuleSpecificSignal.ToSingleNodeSignals => ToSingleNodeSignals;

    /// <summary>
    /// Get a slice of this signal
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public INamedSignal this[Range range] { get; }

    /// <summary>
    /// Indexer for multi-dimensional signals, with type specified as <see cref="ISingleNodeNamedSignal"/>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public new ISingleNodeNamedSignal this[Index index] { get; }

    ISingleNodeSignal ISignal.this[Index index] => this[index];

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

    IModuleSpecificSignal? IModuleSpecificSignal.ParentSignal => ParentSignal;

    INamedSignal IModuleSpecificSignal.AsNamedSignal() => this;

    /// <summary>
    /// Tests if this signal is part of a port in a port mapping, since it can't be directly keyed. 
    /// For example, if this is a vector node in a vector that is a port. 
    /// </summary>
    /// <param name="mapping">Port mapping to look in</param>
    /// <param name="equivalentSignal">The equivalent signal that this is mapped to</param>
    /// <returns></returns>
    public bool IsPartOfPortMapping(PortMapping mapping, [MaybeNullWhen(false)] out INamedSignal equivalentSignal);
}