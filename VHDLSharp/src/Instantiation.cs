namespace VHDLSharp;

/// <summary>
/// Instantiation of one module inside of another (parent)
/// </summary>
/// <param name="module">module that is instantiated</param>
/// <param name="parent">the module inside which this instantiation exists</param>
public class Instantiation(Module module, Module parent)
{
    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public Module Module { get; private init; } = module;

    /// <summary>
    /// Mapping of module's ports to parent's signals (connections to module)
    /// </summary>
    public PortMapping PortMapping { get; private init; } = new(module, parent);

    /// <summary>
    /// Module inside which the module is instantiated
    /// </summary>
    public Module Parent { get; private init; } = parent;
}