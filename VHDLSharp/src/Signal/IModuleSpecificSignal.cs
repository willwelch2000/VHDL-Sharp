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

    /// <summary>
    /// Top-level signal, as type <see cref="IModuleSpecificSignal"/>.
    /// If this is the top level, it returns this. 
    /// Otherwise, it goes up in hierarchy as much as possible
    /// </summary>
    public new IModuleSpecificSignal TopLevelSignal
    {
        get
        {
            IModuleSpecificSignal signal = this;
            while (signal.ParentSignal is not null)
                signal = signal.ParentSignal;
            return signal;
        }
    }

    ISignal ISignal.TopLevelSignal => TopLevelSignal;
    
    /// <summary>
    /// If this is part of a larger group (e.g. vector node), get the parent signal (one layer up)
    /// </summary>
    public new IModuleSpecificSignal? ParentSignal { get; }

    ISignal? ISignal.ParentSignal => ParentSignal;
}