using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Behaviors;
using VHDLSharp.Signals;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// A digital module--a circuit that has some functionality
/// </summary>
public class Module : IModule, IValidityManagedEntity
{
    private static bool ignoreValidity = false;

    private readonly ValidityManager validityManager;

    private readonly ObservableCollection<object> childrenEntities;

    private readonly ObservableCollection<object> additionalObservedEntities;

    private EventHandler? updated;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Module()
    {
        // The collection callbacks are considerd part of the objects' responsibilities
        // and include throwing exceptions when needed
        Ports.CollectionChanged += PortsListUpdated;
        SignalBehaviors.CollectionChanged += BehaviorsListUpdated;
        Instantiations = new(this);

        // Initialize validity manager and list of tracked entities
        childrenEntities = [Instantiations];
        additionalObservedEntities = [];
        validityManager = new ValidityManager<object>(this, childrenEntities, additionalObservedEntities);
    }

    /// <summary>
    /// Constructor with name
    /// </summary>
    /// <param name="name"></param>
    public Module(string name) : this()
    {
        Name = name;
    }

    /// <summary>
    /// Construct module given port names and directions
    /// </summary>
    /// <param name="ports">Tuples of name and direction for port</param>
    public Module(IEnumerable<(string, PortDirection)> ports) : this()
    {
        foreach ((string name, PortDirection direction) in ports)
            AddNewPort(name, direction);
    }

    /// <summary>
    /// Construct module given port names and directions
    /// </summary>
    /// <param name="name">Name for module</param>
    /// <param name="ports">Tuples of name and direction for port</param>
    public Module(string name, IEnumerable<(string, PortDirection)> ports) : this(name)
    {
        foreach ((string portName, PortDirection direction) in ports)
            AddNewPort(portName, direction);
    }
    
    /// <summary>
    /// Event called when a property of this module (not a child) is changed 
    /// that could affect other objects, such as port mapping
    /// </summary>
    public event EventHandler? Updated
    {
        add
        {
            updated -= value; // remove if already present
            updated += value;
        }
        remove => updated -= value;
    }

    /// <inheritdoc/>
    public ValidityManager ValidityManager => validityManager;

    /// <summary>
    /// Name of the module
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Mapping of module signal to behavior that defines it.
    /// To remove a behavior, remove kvp.
    /// Changes are rejected if they cause an error during validation or in any method linked to the module's update events
    /// </summary>
    public ObservableDictionary<INamedSignal, IBehavior> SignalBehaviors { get; set; } = [];

    /// <summary>
    /// List of ports for this module. 
    /// Changes are rejected if they cause an error during validation or in any method linked to the module's update events
    /// </summary>
    public ObservableCollection<IPort> Ports { get; } = [];

    /// <summary>
    /// List of module instantiations inside of this module
    /// Changes are rejected if they cause an error during validation or in any method linked to the module's update events
    /// </summary>
    public InstantiationCollection Instantiations { get; }

    /// <summary>
    /// Get all named signals used in this module. 
    /// Signals can come from ports, behavior input signals, or output signals. 
    /// If all of a multi-dimensional signal's children are used, then the top-level signal is included. 
    /// Otherwise, only the children are returned. 
    /// </summary>
    public IEnumerable<INamedSignal> NamedSignals
    {
        get
        {
            // Get list of all single-node named signals used
            HashSet<INamedSignal> allSingleNodeSignals = [.. Ports.Select(p => p.Signal)
                .Union(SignalBehaviors.Values.SelectMany(b => b.NamedInputSignals))
                .Union(SignalBehaviors.Keys)
                .Union(Instantiations.SelectMany(i => i.PortMapping.Values))
                .SelectMany(s => s.ToSingleNodeSignals)];

            HashSet<INamedSignal> topLevelSignals = [.. allSingleNodeSignals.Select(s => s.TopLevelSignal)];
            HashSet<INamedSignal> allNamedSignals = [];

            foreach (INamedSignal topLevelSignal in topLevelSignals)
            {
                // If all child signals are present, add top level one
                if (topLevelSignal.ToSingleNodeSignals.All(allSingleNodeSignals.Contains))
                    allNamedSignals.Add(topLevelSignal);
                else
                    // Otherwise, add single-node signals that are present
                    foreach (ISingleNodeNamedSignal singleNodeSignal in topLevelSignal.ToSingleNodeSignals.Where(allSingleNodeSignals.Contains))
                        allNamedSignals.Add(singleNodeSignal);
            }

            return allNamedSignals;
        }
    }

    /// <summary>
    /// Get all modules (recursive) used by this module as instantiations
    /// </summary>
    public IEnumerable<IModule> ModulesUsed =>
        Instantiations.SelectMany(i => i.InstantiatedModule.ModulesUsed.Append(i.InstantiatedModule)).Distinct();

    /// <summary>
    /// True if module is ready to be used
    /// TODO if I keep this structure where a signal can have > 2 levels of hierarchy, needs to be changed
    /// </summary>
    public bool Complete
    {
        get
        {
            // If any output signal hasn't been assigned
            foreach (IPort port in Ports.Where(p => p.Direction == PortDirection.Output))
            {
                if (SignalBehaviors.ContainsKey(port.Signal)) // Assigned as itself
                    continue;
                if (port.Signal.ToSingleNodeSignals.All(SignalBehaviors.ContainsKey)) // Assigned as all children
                    continue;
                return false;
            }

            return true;
        }
    }

    private bool ConsiderValid => ignoreValidity || ValidityManager.IsValid();

    /// <summary>
    /// Generate a signal with this module as the parent
    /// </summary>
    /// <param name="name">name of the signal</param>
    /// <returns></returns>
    public Signal GenerateSignal(string name) => new(name, this);

    /// <summary>
    /// Generate a vector signal with this module as the parent
    /// </summary>
    /// <param name="name">name of the vector</param>
    /// <param name="dimension">dimension of the vector</param>
    /// <returns></returns>
    public Vector GenerateVector(string name, int dimension) => new(name, this, dimension);

    /// <summary>
    /// Create a port with a new single-dimension signal and add the new port to the list of ports
    /// </summary>
    /// <param name="name"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Port AddNewPort(string name, PortDirection direction)
    {
        Port result = new(new Signal(name, this), direction);
        Ports.Add(result);
        return result;
    }

    /// <summary>
    /// Create a port with a new signal of a given dimension and add the new port to the list of ports
    /// </summary>
    /// <param name="name"></param>
    /// <param name="dimension">Dimension for new signal</param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Port AddNewPort(string name, int dimension, PortDirection direction)
    {
        NamedSignal signal = dimension == 1 ? new Signal(name, this) : new Vector(name, this, dimension);
        Port result = new(signal, direction);
        Ports.Add(result);
        return result;
    }

    /// <summary>
    /// Create a port with a signal and add the new port to the list of ports
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Port AddNewPort(INamedSignal signal, PortDirection direction)
    {
        if (signal.ParentModule != this)
            throw new ArgumentException("Signal must have this module as parent");
        
        Port result = new(signal, direction);
        Ports.Add(result);
        return result;
    }

    /// <summary>
    /// Add new instantiation automatically using this module as parent module
    /// </summary>
    /// <param name="module">Module to be instantiated in this</param>
    /// <param name="name">Name of instantiation</param>
    /// <returns></returns>
    public IInstantiation AddNewInstantiation(Module module, string name) => Instantiations.Add(module, name);

    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;

    /// <summary>
    /// Get the module as a VHDL string, including all modules used
    /// </summary>
    /// <returns></returns>
    public string GetVhdl()
    {
        if (ConsiderValid || !Complete)
            throw new Exception("Module not yet complete");

        StringBuilder sb = new();

        // Header
        sb.AppendLine("library ieee");
        sb.AppendLine("use ieee.std_logic_1164.all;\n");

        // Submodules
        ignoreValidity = true; // Subcircuits and this already checked
        foreach (var module in ModulesUsed)
        {
            sb.AppendLine(module.GetVhdlNoSubmodules());
            sb.AppendLine();
        }

        // Main module
        sb.AppendLine(GetVhdlNoSubmodules());
        ignoreValidity = false;

        return sb.ToString();
    }

    /// <summary>
    /// Get the VHDL for this module without submodules or 
    /// stuff that goes at the beginning of the file
    /// </summary>
    /// <returns></returns>
    public string GetVhdlNoSubmodules()
    {
        if (!ConsiderValid || !Complete)
            throw new Exception("Module not yet complete");

        StringBuilder sb = new();

        // Entity statement
        sb.AppendLine($"entity {Name} is");
        sb.AppendLine("\tport (");
        sb.AppendJoin(";\n", Ports.Select(p => p.GetVhdlDeclaration().AddIndentation(2)));
        sb.AppendLine();
        sb.AppendLine(");".AddIndentation(1));
        sb.AppendLine($"end {Name};");

        // Architecture
        sb.AppendLine();
        sb.AppendLine($"architecture rtl of {Name} is");

        // Signals
        foreach(INamedSignal signal in NamedSignals.Except(Ports.Select(p => p.Signal)))
        {
            sb.AppendLine(signal.GetVhdlDeclaration().AddIndentation(1));
        }

        // Component declarations
        foreach (IModule module in ModulesUsed)
            sb.AppendLine(module.GetVhdlComponentDeclaration());

        // Begin
        sb.AppendLine("begin");

        // Add all instantiations
        sb.Append(Instantiations.GetVhdl().AddIndentation(1));

        // Behaviors
        foreach ((INamedSignal outputSignal, IBehavior behavior) in SignalBehaviors)
        {
            sb.AppendLine(behavior.GetVhdlStatement(outputSignal).AddIndentation(1));
        }

        // End
        sb.AppendLine("end rtl;");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string GetSpice() => GetSpice(false);

    /// <summary>
    /// Convert module to Spice circuit
    /// </summary>
    /// <param name="subcircuit">Whether it should be wrapped in a subcircuit or top-level</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string GetSpice(bool subcircuit)
    {
        if (!ConsiderValid || !Complete)
            throw new Exception("Module not yet complete");

        StringBuilder sb = new();
        
        // Start subcircuit
        if (subcircuit)
            sb.AppendLine($".subckt {Name} {string.Join(' ', PortsToSpice())}\n");

        int indentation = subcircuit ? 1 : 0;

        foreach (string subcircuitDeclaration in Instantiations.GetSpiceSubcircuitDeclarations())
            sb.AppendLine(subcircuitDeclaration.AddIndentation(indentation));

        // Add VDD node and PMOS/NMOS models
        sb.AppendLine($"V_VDD VDD 0 {Util.VDD}".AddIndentation(indentation));
        sb.AppendLine($".MODEL {Util.NmosModelName} NMOS".AddIndentation(indentation));
        sb.AppendLine($".MODEL {Util.PmosModelName} PMOS".AddIndentation(indentation));

        // Add all instantiations
        sb.Append(Instantiations.GetSpiceInstantiationStatements().AddIndentation(indentation));

        // Add behaviors
        int i = 0;
        foreach ((INamedSignal signal, IBehavior behavior) in SignalBehaviors)
            sb.AppendLine(behavior.GetSpice(signal, i++.ToString()).AddIndentation(indentation));
        
        // Add large resistors from output/bidirectional ports to ground
        foreach (INamedSignal signal in Ports.Where(p => p.Direction == PortDirection.Output || p.Direction == PortDirection.Bidirectional).Select(p => p.Signal))
        {
            int j = 0;
            foreach (ISingleNodeNamedSignal singleNodeSignal in signal.ToSingleNodeSignals)
                sb.AppendLine($"R{Util.GetSpiceName(i++.ToString(), j++, "floating")} {singleNodeSignal.GetSpiceName()} 0 1e9");
        }

        // End subcircuit
        if (subcircuit)
            sb.AppendLine($".ends {Name}");

        return sb.ToString();
    }

    /// <summary>
    /// All ports converted to Spice strings
    /// </summary>
    /// <returns></returns>
    private IEnumerable<string> PortsToSpice() => Ports.SelectMany(p => p.Signal.ToSingleNodeSignals).Select(s => s.GetSpiceName());

    /// <summary>
    /// Convert module to Spice# <see cref="SubcircuitDefinition"/> object
    /// </summary>
    /// <returns></returns>
    public SubcircuitDefinition GetSpiceSharpSubcircuit()
    {
        if (!ConsiderValid || !Complete)
            throw new Exception("Module not yet complete");

        EntityCollection entities = [];
        string[] pins = [.. Ports.SelectMany(p => p.Signal.ToSingleNodeSignals).Select(s => s.GetSpiceName())];

        // Add VDD node and PMOS/NMOS models
        entities.Add(new VoltageSource("V_VDD", "VDD", "0", Util.VDD));
        Mosfet1Model nmosModel = new(Util.NmosModelName);
        nmosModel.Parameters.SetNmos(true);
        Mosfet1Model pmosModel = new(Util.PmosModelName);
        pmosModel.Parameters.SetPmos(true);
        entities.Add(nmosModel);
        entities.Add(pmosModel);

        // Add instantiations
        foreach (IEntity entity in Instantiations.GetSpiceSharpEntities())
            entities.Add(entity);

        // Add behaviors
        int i = 0;
        foreach ((INamedSignal signal, IBehavior behavior) in SignalBehaviors)
        {
            foreach (IEntity entity in behavior.GetSpiceSharpEntities(signal, i++.ToString()))
                entities.Add(entity);
        }
        
        // Add large resistors from output/bidirectional ports to ground
        foreach (INamedSignal signal in Ports.Where(p => p.Direction == PortDirection.Output || p.Direction == PortDirection.Bidirectional).Select(p => p.Signal))
        {
            int j = 0;
            foreach (ISingleNodeNamedSignal singleNodeSignal in signal.ToSingleNodeSignals)
                entities.Add(new Resistor($"R{Util.GetSpiceName(i++.ToString(), j++, "floating")}", singleNodeSignal.GetSpiceName(), "0", 1e9));
        }

        return new(entities, pins);
    }

    /// <summary>
    /// Convert module to Spice# <see cref="Circuit"/> object
    /// </summary>
    /// <returns></returns>
    public Circuit GetSpiceSharpCircuit() => [.. GetSpiceSharpSubcircuit().Entities];

    /// <summary>
    /// Test if the module contains a signal
    /// TODO if I keep this structure where a signal can have > 2 levels of hierarchy, needs to be changed
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public bool ContainsSignal(INamedSignal signal)
    {
        INamedSignal[] namedSignals = [.. NamedSignals];
        // True if directly contained
        if (namedSignals.Contains(signal))
            return true;
        // True if signal is single-node and the parent is contained
        if (signal is ISingleNodeNamedSignal)
            return namedSignals.Contains(signal.TopLevelSignal);
        return false;
    }

    private void BehaviorsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Track each new behavior in validity manager
        if ((e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace) && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
                if (newItem is KeyValuePair<INamedSignal, IBehavior> kvp)
                    childrenEntities.Add(kvp.Value);
        
        // If something has been removed, remove behavior from tracking
        if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset || e.Action == NotifyCollectionChangedAction.Replace) && e.OldItems is not null)
            foreach (object oldItem in e.OldItems)
                if (oldItem is KeyValuePair<INamedSignal, IBehavior> kvp)
                    childrenEntities.Remove(kvp.Value);

        // Invoke module update
        updated?.Invoke(this, e);
    }

    private void PortsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Track each new port, if a validity-managed entity
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
                childrenEntities.Add(newItem);
        
        // If something has been removed, remove from tracking
        if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset) && e.OldItems is not null)
            foreach (object oldItem in e.OldItems)
                childrenEntities.Remove(oldItem);

        updated?.Invoke(this, e);
    }

    /// <inheritdoc/>
    public string GetVhdlComponentDeclaration()
    {
        StringBuilder sb = new();

        // Entity statement
        sb.AppendLine($"component {Name}");
        sb.AppendLine("\tport (");
        sb.AppendJoin(";\n", Ports.Select(p => p.GetVhdlDeclaration().AddIndentation(2)));
        sb.AppendLine();
        sb.AppendLine(");".AddIndentation(1));
        sb.AppendLine($"end component {Name};");

        return sb.ToString();
    }

    /// <inheritdoc/>
    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;
        // Check that behaviors are in correct module/have correct dimension and that output signal isn't input port
        foreach ((INamedSignal outputSignal, IBehavior behavior) in SignalBehaviors)
        {
            if (outputSignal.ParentModule != this)
                exception = new Exception($"Output signal {outputSignal.Name} must have this module ({Name}) as parent");
            if (behavior.ParentModule is not null && behavior.ParentModule != this)
                exception = new Exception($"Behavior must have this module as parent");
            if (!behavior.IsCompatible(outputSignal))
                exception = new Exception($"Behavior must be compatible with output signal");
            if (Ports.Where(p => p.Direction == PortDirection.Input).Select(p => p.Signal).Contains(outputSignal))
                exception = new Exception($"Output signal ({outputSignal}) must not be an input port");
        }

        // Throw error if a signal has two assignments
        List<ISingleNodeNamedSignal> allSingleNodeOutputSignals = [.. SignalBehaviors.SelectMany(kvp => kvp.Key.ToSingleNodeSignals)];
        if (allSingleNodeOutputSignals.Count != allSingleNodeOutputSignals.Distinct().Count())
            exception = new Exception("Module defines an overlapping parent and child output signal");

        // Don't allow ports with the same signal or with wrong parent module
        HashSet<ISignal> portSignals = [];
        if (!Ports.All(p => portSignals.Add(p.Signal) && p.Signal.ParentModule == this))
        {
            string? duplicate = Ports.FirstOrDefault(p => Ports.Count(p2 => p.Signal == p2.Signal) > 1)?.Signal?.Name;
            if (duplicate is not null)
                exception = new Exception($"The same signal (\"{duplicate}\") cannot be added as two different ports");
            else
                exception = new Exception("Port signals must have this module as parent");
        }

        return exception is null;
    }
}
