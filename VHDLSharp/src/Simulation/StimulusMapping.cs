using VHDLSharp.Modules;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Exception related to mapping ports
/// </summary>
public class StimulusMappingException : Exception
{
    /// <summary>
    /// Parameterless constructor
    /// </summary>
    public StimulusMappingException() : base("A stimulus mapping exception has occurred.")
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message
    /// </summary>
    /// <param name="message"></param>
    public StimulusMappingException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor that accepts a custom message and inner exception
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public StimulusMappingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Mapping of ports of a module to stimuli set for a simulation
/// </summary>
public class StimulusMapping : ObservableDictionary<Port, IStimulusSet>
{
    private readonly Module module;
    
    /// <summary>
    /// Construct port mapping given module that has stimuli applied to its ports
    /// </summary>
    /// <param name="module">Module that has stimuli applied to its ports</param>
    public StimulusMapping(Module module)
    {
        this.module = module;
        this.module.ModuleUpdated += ModuleUpdated;
    }

    /// <summary>
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<Port> PortsToAssign => module.Ports.Except(Keys);

    /// <inheritdoc/>
    public override IStimulusSet this[Port port]
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
        foreach ((Port port, IStimulusSet stimulus) in this)
        {
            if (!(port.Direction == PortDirection.Input || port.Direction == PortDirection.Bidirectional))
                throw new StimulusMappingException($"Port {port} must be input or bidirectional");
            if (!port.Signal.Dimension.Compatible(stimulus.Dimension))
                throw new StimulusMappingException($"Port {port} and signal {stimulus} must have the same dimension");
            if (port.Signal.ParentModule != module)
                throw new StimulusMappingException($"Ports must have the specified module {module} as parent");
            if (!module.Ports.Contains(port))
                throw new StimulusMappingException($"Port {port} must be in the list of ports of specified module {module}");
        }
    }

    /// <summary>
    /// True if port mapping is complete (all input ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool Complete() => module.Ports.Where(p => p.Direction == PortDirection.Input).All(ContainsKey);

    /// <inheritdoc/>
    public override void Add(Port port, IStimulusSet stimulus)
    {
        base.Add(port, stimulus);
        CheckValid();
    }
}