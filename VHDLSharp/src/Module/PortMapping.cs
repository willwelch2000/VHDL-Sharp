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
/// Changes are rejected if they cause a validation error (or an exception in anything linked to IValidityManagedEntity.Updated)
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
        CollectionChanged += HandleCollectionChanged;
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
    
    private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            updated?.Invoke(this, e);
        }
        catch
        {
            // Undo anything added
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
                foreach (object newItem in e.NewItems)
                    if (newItem is KeyValuePair<IPort, INamedSignal> kvp)
                        Remove(kvp);

            // Undo anything replaced
            if (e.Action == NotifyCollectionChangedAction.Replace && e.OldItems is not null)
                foreach (object oldItem in e.OldItems)
                    if (oldItem is KeyValuePair<IPort, INamedSignal> kvp)
                        this[kvp.Key] = kvp.Value;
            throw;
        }
    }
}