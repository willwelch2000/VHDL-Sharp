using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Signals;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

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
        manager = new ValidityManager<object>(this, trackedEntities);
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

    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)] out string explanation)
    {
        foreach ((IPort port, INamedSignal signal) in this)
        {
            if (!port.Signal.Dimension.Compatible(signal.Dimension))
            {
                explanation = "Port {port} and signal {signal} must have the same dimension";
                return false;
            }
            if (port.Signal.ParentModule != InstantiatedModule)
            {
                explanation = "Ports must have the specified module ({InstantiatedModule}) as parent";
                return false;
            }
            if (!InstantiatedModule.Ports.Contains(port))
            {
                explanation = "Port {port} must be in the list of ports of specified module {InstantiatedModule}";
                return false;
            }
            if (signal.ParentModule != ParentModule)
            {
                explanation = "Signal must have module {ParentModule} as parent";
                return false;
            }
            if (port.Direction == PortDirection.Output && ParentModule.Ports.Any(p => p.Signal == signal && p.Direction == PortDirection.Input))
            {
                explanation = "Output port cannot be assigned to parent module's input port";
                return false;
            }
        }
        explanation = null;
        return true;
    }

    /// <summary>
    /// True if port mapping is complete (all ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool IsComplete() => InstantiatedModule.Ports.All(ContainsKey);
}