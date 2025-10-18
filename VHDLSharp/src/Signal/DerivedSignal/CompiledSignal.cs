using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// A signal that is created during derived signal compilation as a linked signal
/// </summary>
public class CompiledSignal : Signal, ICompiledObject
{
    internal CompiledSignal(string name, IModule parent) : base(name, parent) {}
}