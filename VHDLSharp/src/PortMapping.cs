namespace VHDLSharp;

/// <summary>
/// Mapping of a port to 
/// </summary>
public class PortMapping : IDictionary<Port, Signal>
{
    private Module module;

    private Module parent;
    
    private Dictionary<Port, Signal> backendDictionary;

    /// <summary>
    /// Construct port mapping given instantiated module and parent module
    /// </summary>
    /// <param name="module">module that is instantiated</param>
    /// <param name="parent">module that contains instantiated module</param>
    public PortMapping(Module module, Module parent)
    {
        this.module = module;
        this.module.ModuleUpdated += ModuleUpdated;
        this.parent = parent;
        backendDictionary = [];
    }

    private void ModuleUpdated(object? sender, EventArgs eventArgs)
    {
        CheckValid();
    }

    private void CheckValid()
    {
        foreach ((Port port, Signal signal) in backendDictionary)
        {
            if (port.Signal.Parent != module)
                throw new Exception($"Ports must have the specified module {module} as parent");
            if (!module.Ports.Contains(port))
                throw new Exception($"Port {port} must be in the list of ports of specified module {module}");
            if (signal.Parent != parent)
                throw new Exception($"Signal must have module {parent} as parent");
        }
    }

    // Add stuff to automatically check validity
}