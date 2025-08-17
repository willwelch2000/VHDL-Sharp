using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any signal that is assigned to a specific <see cref="IModule"/>
/// </summary>
public interface ISignalWithAssignedModule : ISignal
{
    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public IModule ParentModule { get; }
}