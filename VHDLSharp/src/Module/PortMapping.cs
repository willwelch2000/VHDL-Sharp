using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Modules;

/// <summary>
/// Exception related to mapping ports
/// </summary>
public class PortMappingException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public PortMappingException() : base("A port mapping exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public PortMappingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public PortMappingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Mapping of ports of a module to the signals its connected to in an instantiation
/// </summary>
public class PortMapping : ObservableDictionary<Port, NamedSignal>
{
    private readonly Module module;

    private readonly Module parentModule;
    
    /// <summary>
    /// Construct port mapping given instantiated module and parent module
    /// </summary>
    /// <param name="module">module that is instantiated</param>
    /// <param name="parentModule">module that contains instantiated module</param>
    public PortMapping(Module module, Module parentModule)
    {
        this.module = module;
        this.module.ModuleUpdated += ModuleUpdated;
        this.parentModule = parentModule;
    }

    /// <summary>
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<Port> PortsToAssign => module.Ports.Except(Keys);

    /// <summary>
    /// Indexer for port mapping
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public override NamedSignal this[Port port]
    {
        get => base[port];
        set
        {
            base[port] = value;
            CheckValid();
        }
    }

    private void ModuleUpdated(object? sender, EventArgs eventArgs)
    {
        CheckValid();
    }

    private void CheckValid()
    {
        foreach ((Port port, NamedSignal signal) in this)
        {
            if (!port.Signal.Dimension.Compatible(signal.Dimension))
                throw new PortMappingException($"Port {port} and signal {signal} must have the same dimension");
            if (port.Signal.ParentModule != module)
                throw new PortMappingException($"Ports must have the specified module {module} as parent");
            if (!module.Ports.Contains(port))
                throw new PortMappingException($"Port {port} must be in the list of ports of specified module {module}");
            if (signal.ParentModule != parentModule)
                throw new PortMappingException($"Signal must have module {parentModule} as parent");
        }
    }

    /// <summary>
    /// True if port mapping is complete (all ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool Complete() => module.Ports.All(ContainsKey);

    /// <inheritdoc/>
    public override void Add(Port port, NamedSignal signal)
    {
        base.Add(port, signal);
        CheckValid();
    }
}