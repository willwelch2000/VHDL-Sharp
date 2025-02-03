using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// A signal used in a module
/// </summary>
/// <param name="name">name of signal</param>
/// <param name="parent">module to which this signal belongs</param>
public class Signal(string name, Module parent) : SingleNodeNamedSignal
{
    /// <summary>
    /// Name of the signal
    /// </summary>
    public override string Name => name;

    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public override Module ParentModule => parent;

    /// <inheritdoc/>
    public override NamedSignal? ParentSignal => null;

    /// <inheritdoc/>
    public override NamedSignal TopLevelSignal => this;

    /// <inheritdoc/>
    public override string ToSpice() => Name;
}