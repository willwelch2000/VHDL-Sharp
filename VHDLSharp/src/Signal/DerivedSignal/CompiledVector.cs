using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// A vector that is created during derived signal compilation as a linked signal
/// </summary>
public class CompiledVector : Vector, ICompiledObject
{
    internal CompiledVector(string name, IModule parent, int dimension) : base(name, parent, dimension) {}
}