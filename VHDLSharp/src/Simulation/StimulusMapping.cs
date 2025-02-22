using VHDLSharp.Modules;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

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
public class StimulusMapping : ObservableDictionary<IPort, IStimulusSet>
{
    private readonly IModule module;
    
    /// <summary>
    /// Construct port mapping given module that has stimuli applied to its ports
    /// </summary>
    /// <param name="module">Module that has stimuli applied to its ports</param>
    public StimulusMapping(IModule module)
    {
        this.module = module;
        if (module is IValidityManagedEntity moduleAsEntity)
            moduleAsEntity.Updated += (sender, e) => CheckValid();
    }

    /// <summary>
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<IPort> PortsToAssign => module.Ports.Except(Keys);

    /// <inheritdoc/>
    public override IStimulusSet this[IPort port]
    {
        get => base[port];
        set
        {
            IStimulusSet? prevVal = TryGetValue(port, out var val) ? val : null;
            base[port] = value;

            // If error is caused by CheckValid, undo it
            try
            {
                CheckValid();
            }
            catch (Exception)
            {
                if (prevVal is null)
                    Remove(port);
                else
                    base[port] = prevVal;
                throw;
            }
        }
    }

    private void CheckValid()
    {
        foreach ((IPort port, IStimulusSet stimulus) in this)
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
    public bool IsComplete() => module.Ports.Where(p => p.Direction == PortDirection.Input).All(ContainsKey);

    /// <inheritdoc/>
    public override void Add(IPort port, IStimulusSet stimulus)
    {
        IStimulusSet? prevVal = TryGetValue(port, out var val) ? val : null;
        base.Add(port, stimulus);

        // If error is caused by CheckValid, undo it
        try
        {
            CheckValid();
        }
        catch (Exception)
        {
            if (prevVal is null)
                Remove(port);
            else
                base[port] = prevVal;
            throw;
        }
    }
}