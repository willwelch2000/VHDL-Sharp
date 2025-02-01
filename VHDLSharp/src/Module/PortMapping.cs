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
/// Mapping of ports of a module to the signals it's connected to in an instantiation
/// </summary>
public class PortMapping : ObservableDictionary<Port, NamedSignal>, IHasParentModule
{
    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public Module InstantiatedModule { get; }

    /// <summary>
    /// Module that contains module instantiation
    /// </summary>
    public Module ParentModule { get; }
    
    /// <summary>
    /// Construct port mapping given instantiated module and parent module
    /// </summary>
    /// <param name="instantiatedModule">Module that is instantiated</param>
    /// <param name="parentModule">Module that contains instantiated module</param>
    public PortMapping(Module instantiatedModule, Module parentModule)
    {
        InstantiatedModule = instantiatedModule;
        InstantiatedModule.ModuleUpdated += ModuleUpdated;
        ParentModule = parentModule;
    }

    /// <summary>
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<Port> PortsToAssign => InstantiatedModule.Ports.Except(Keys);

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
            if (port.Signal.ParentModule != InstantiatedModule)
                throw new PortMappingException($"Ports must have the specified module ({InstantiatedModule}) as parent");
            if (!InstantiatedModule.Ports.Contains(port))
                throw new PortMappingException($"Port {port} must be in the list of ports of specified module {InstantiatedModule}");
            if (signal.ParentModule != ParentModule)
                throw new PortMappingException($"Signal must have module {ParentModule} as parent");
        }
    }

    /// <summary>
    /// True if port mapping is complete (all ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool IsComplete() => InstantiatedModule.Ports.All(ContainsKey);

    /// <inheritdoc/>
    public override void Add(Port port, NamedSignal signal)
    {
        base.Add(port, signal);
        CheckValid();
    }
}