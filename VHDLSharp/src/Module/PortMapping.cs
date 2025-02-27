using System.Collections.ObjectModel;
using System.Collections.Specialized;
using VHDLSharp.Signals;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

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
/// Mapping of ports of a module to the signals it's connected to in an instantiation.
/// Handles throwing exceptions
/// </summary>
public class PortMapping : ObservableDictionary<IPort, INamedSignal>, IValidityManagedEntity
{
    private readonly ValidityManager manager;

    private readonly ObservableCollection<object> trackedEntities;
    
    private EventHandler? updated;

    /// <summary>
    /// Construct port mapping given instantiated module and parent module
    /// </summary>
    /// <param name="instantiatedModule">Module that is instantiated</param>
    /// <param name="parentModule">Module that contains instantiated module</param>
    public PortMapping(IModule instantiatedModule, IModule parentModule)
    {
        InstantiatedModule = instantiatedModule;
        ParentModule = parentModule;
        trackedEntities = [instantiatedModule, parentModule];
        manager = new(this, trackedEntities);
        CollectionChanged += (s, e) => updated?.Invoke(this, e);
    }

    ValidityManager IValidityManagedEntity.ValidityManager => manager;

    /// <summary>
    /// Event called when mapping is updated
    /// </summary>
    event EventHandler? IValidityManagedEntity.Updated
    {
        add
        {
            updated -= value; // remove if already present
            updated += value;
        }
        remove => updated -= value;
    }

    /// <summary>
    /// Module that is instantiated
    /// </summary>
    public IModule InstantiatedModule { get; }

    /// <summary>
    /// Module that contains module instantiation
    /// </summary>
    public IModule ParentModule { get; }

    /// <summary>
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<IPort> PortsToAssign => InstantiatedModule.Ports.Except(Keys);

    /// <summary>
    /// Indexer for port mapping
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public override INamedSignal this[IPort port]
    {
        get => base[port];
        set
        {
            INamedSignal? prevVal = TryGetValue(port, out var val) ? val : null;

            // If error is caused by update (called by ObservableDictionary), undo it
            try
            {
                base[port] = value;
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

    void IValidityManagedEntity.CheckValidity()
    {
        foreach ((IPort port, INamedSignal signal) in this)
        {
            if (!port.Signal.Dimension.Compatible(signal.Dimension))
                throw new PortMappingException($"Port {port} and signal {signal} must have the same dimension");
            if (port.Signal.ParentModule != InstantiatedModule)
                throw new PortMappingException($"Ports must have the specified module ({InstantiatedModule}) as parent");
            if (!InstantiatedModule.Ports.Contains(port))
                throw new PortMappingException($"Port {port} must be in the list of ports of specified module {InstantiatedModule}");
            if (signal.ParentModule != ParentModule)
                throw new PortMappingException($"Signal must have module {ParentModule} as parent");
            if (port.Direction == PortDirection.Output && ParentModule.Ports.Any(p => p.Signal == signal && p.Direction == PortDirection.Input))
                throw new PortMappingException($"Output port cannot be assigned to parent module's input port");
        }
    }

    /// <summary>
    /// True if port mapping is complete (all ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool IsComplete() => InstantiatedModule.Ports.All(ContainsKey);

    /// <inheritdoc/>
    public override void Add(IPort port, INamedSignal signal)
    {
        INamedSignal? prevVal = TryGetValue(port, out var val) ? val : null;

        // If error is caused by update (called by ObservableDictionary), undo it
        try
        {
            base.Add(port, signal);
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