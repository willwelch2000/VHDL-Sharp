namespace VHDLSharp;

/// <summary>
/// Instantiation of one module inside of another
/// </summary>
public class Instantiation
{
    private readonly List<Signal> portConnections;

    /// <summary>
    /// Create an instantiation of a given module in another module
    /// </summary>
    /// <param name="module">module that is instantiated</param>
    /// <param name="portConnections">the nodes inside parent that are connected to the ports</param>
    /// <param name="parent">the module inside which this instantiation exists</param>
    /// <exception cref="ArgumentException"></exception>
    public Instantiation(Module module, IEnumerable<Signal> portConnections, Module parent)
    {
        Module = module;
        this.portConnections = new(portConnections);
        Parent = parent;

        if (portConnections.Select(p => p.Parent).Distinct().Union([parent]).Count() > 1)
            throw new ArgumentException("All port connections should have the same parent as this instantiation");
    }

    public Module Module { get; private set; }

    public Dictionary<Port, Signal> PortConnections => [];

    public Module Parent { get; private set; }

    public bool CheckValid()
    {
        return true;
    }
}