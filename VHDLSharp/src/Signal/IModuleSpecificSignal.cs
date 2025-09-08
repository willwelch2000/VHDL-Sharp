using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any signal that is assigned to a specific <see cref="IModule"/>
/// </summary>
public interface IModuleSpecificSignal : ISignal
{
    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public IModule ParentModule { get; }

    /// <summary>
    /// If this has a dimension > 1, convert to a list of named signals with dimension 1. 
    /// If it is a single-node signal, then return itself
    /// </summary>
    public new IEnumerable<ISingleNodeModuleSpecificSignal> ToSingleNodeSignals { get; }

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;
}