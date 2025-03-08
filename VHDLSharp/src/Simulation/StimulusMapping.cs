using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
public class StimulusMapping : ObservableDictionary<IPort, IStimulusSet>, IValidityManagedEntity
{
    private readonly ValidityManager manager;

    private readonly ObservableCollection<object> trackedEntities;
    
    private EventHandler? updated;
    
    private readonly IModule module;
    
    /// <summary>
    /// Construct port mapping given module that has stimuli applied to its ports
    /// </summary>
    /// <param name="module">Module that has stimuli applied to its ports</param>
    public StimulusMapping(IModule module)
    {
        this.module = module;
        trackedEntities = [module];
        manager = new ValidityManager<object>(this, [], trackedEntities);
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
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<IPort> PortsToAssign => module.Ports.Except(Keys);

    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;
        foreach ((IPort port, IStimulusSet stimulus) in this)
        {
            if (!(port.Direction == PortDirection.Input || port.Direction == PortDirection.Bidirectional))
                exception = new StimulusMappingException($"Port {port} must be input or bidirectional");
            if (!port.Signal.Dimension.Compatible(stimulus.Dimension))
                exception = new StimulusMappingException($"Port {port} and signal {stimulus} must have the same dimension");
            if (port.Signal.ParentModule != module)
                exception = new StimulusMappingException($"Ports must have the specified module {module} as parent");
            if (!module.Ports.Contains(port))
                exception = new StimulusMappingException($"Port {port} must be in the list of ports of specified module {module}");
        }

        return exception is null;
    }

    /// <summary>
    /// True if port mapping is complete (all input ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool IsComplete() => module.Ports.Where(p => p.Direction == PortDirection.Input).All(ContainsKey);
    
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
                    if (newItem is KeyValuePair<IPort, IStimulusSet> kvp)
                        Remove(kvp);

            // Undo anything replaced
            if (e.Action == NotifyCollectionChangedAction.Replace && e.OldItems is not null)
                foreach (object oldItem in e.OldItems)
                    if (oldItem is KeyValuePair<IPort, IStimulusSet> kvp)
                        this[kvp.Key] = kvp.Value;

            // Remove/clear/move action shouldn't cause issues

            throw;
        }
    }
}