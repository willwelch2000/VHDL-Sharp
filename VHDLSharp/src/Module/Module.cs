using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Behaviors;
using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Modules;

/// <summary>
/// A digital module--a circuit that has some functionality
/// </summary>
public class Module
{
    private EventHandler? moduleUpdated;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Module()
    {
        Ports.CollectionChanged += InvokeModuleUpdated;
        SignalBehaviors.CollectionChanged += InvokeModuleUpdated;
        SignalBehaviors.CollectionChanged += BehaviorsListUpdated;
        Instantiations.CollectionChanged += InvokeModuleUpdated;
        Instantiations.CollectionChanged += InstantiationsListUpdated;
    }

    /// <summary>
    /// Construct module given port names and directions
    /// </summary>
    /// <param name="ports">tuple of name and direction for port</param>
    public Module(IEnumerable<(string, PortDirection)> ports) : this()
    {
        foreach ((string name, PortDirection direction) in ports)
            AddNewPort(name, direction);
    }

    private void InvokeModuleUpdated(object? sender, EventArgs e)
    {
        CheckValid();
        moduleUpdated?.Invoke(this, e);
    }

    private void BehaviorsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Add InvokeModuleUpdated to each new behavior
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
                if (newItem is (NamedSignal outputSignal, DigitalBehavior behavior))
                    behavior.BehaviorUpdated += InvokeModuleUpdated;
    }

    private void InstantiationsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Add InvokeModuleUpdated to each new instantiation
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
            {
                // Don't allow duplicate instantiations in the list
                if (Instantiations.Count(i => i == newItem) > 1)
                    throw new Exception($"The same instantiation ({newItem}) should not be added twice");
                if (newItem is Instantiation instantiation)
                    instantiation.SubmoduleUpdated += InvokeModuleUpdated;
            }
    }


    /// <summary>
    /// Event called when a property of the module is changed that could affect other objects,
    /// such as port mapping
    /// </summary>
    public event EventHandler? ModuleUpdated
    {
        add
        {
            moduleUpdated -= value; // remove if already present
            moduleUpdated += value;
        }
        remove => moduleUpdated -= value;
    }

    /// <summary>
    /// Name of the module
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Mapping of output signal to behavior that defines it
    /// </summary>
    public ObservableDictionary<NamedSignal, DigitalBehavior> SignalBehaviors { get; set; } = [];

    /// <summary>
    /// List of ports for this module
    /// </summary>
    public ObservableCollection<Port> Ports { get; } = [];

    /// <summary>
    /// List of module instantiations inside of this module
    /// </summary>
    public ObservableCollection<Instantiation> Instantiations { get; set; } = [];

    /// <summary>
    /// Get all named signals used in this module
    /// Signals can come from ports, behavior input signals, or output signals
    /// </summary>
    public IEnumerable<NamedSignal> NamedSignals =>
        Ports.Select(p => p.Signal)
        .Union(SignalBehaviors.Values.SelectMany(b => b.NamedInputSignals))
        .Union(SignalBehaviors.Keys)
        .Union(Instantiations.SelectMany(i => i.PortMapping.Values))
        .Select(s => s.TopLevelSignal).Distinct();

    /// <summary>
    /// Get all modules (recursive) used by this module as instantiations
    /// </summary>
    public IEnumerable<Module> ModulesUsed =>
        Instantiations.SelectMany(i => i.InstantiatedModule.ModulesUsed.Append(i.InstantiatedModule)).Distinct();

    /// <summary>
    /// True if module is ready to be used
    /// </summary>
    public bool Complete
    {
        get
        {
            // If any output signal hasn't been assigned
            if (Ports.Where(p => p.Direction == PortDirection.Output).Any(p => !SignalBehaviors.ContainsKey(p.Signal)))
                return false;
            return true;
        }
    }

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
    /// Create a port with a new signal and add the new port to the list of ports
    /// </summary>
    /// <param name="name"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Port AddNewPort(string name, PortDirection direction)
    {
        Port result = new()
        {
            Signal = new Signal(name, this),
            Direction = direction
        };
        Ports.Add(result);
        return result;
    }

    /// <summary>
    /// Create a port with a signal and add the new port to the list of ports
    /// </summary>
    /// <param name="signal"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Port AddNewPort(NamedSignal signal, PortDirection direction)
    {
        if (signal.ParentModule != this)
            throw new ArgumentException("Signal must have this module as parent");
        
        Port result = new()
        {
            Signal = signal,
            Direction = direction
        };
        Ports.Add(result);
        return result;
    }

    /// <summary>
    /// Add new instantiation automatically using this module as parent module
    /// </summary>
    /// <param name="module">Module to be instantiated in this</param>
    /// <param name="name">Name of instantiation</param>
    /// <returns></returns>
    public Instantiation AddNewInstantiation(Module module, string name)
    {
        Instantiation instantiation = new(module, this, name);
        Instantiations.Add(instantiation);
        return instantiation;
    }

    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;

    private void CheckValid()
    {
        // Check that behaviors are in correct module/have correct dimension and that output signal isn't input port
        foreach ((NamedSignal outputSignal, DigitalBehavior behavior) in SignalBehaviors)
        {
            if (outputSignal.ParentModule != this)
                throw new Exception($"Output signal {outputSignal.Name} must have this module ({Name}) as parent");
            if (behavior.ParentModule is not null && behavior.ParentModule != this)
                throw new Exception($"Behavior must have this module as parent");
            if (!behavior.Dimension.Compatible(outputSignal.Dimension))
                throw new Exception("Behavior must have same dimension as assigned output signal");
            if (Ports.Where(p => p.Direction == PortDirection.Input).Select(p => p.Signal).Contains(outputSignal))
                throw new Exception($"Output signal ({outputSignal}) must not be an input port");
        }
    }

    /// <summary>
    /// Get the module as a VHDL string, including all modules used
    /// </summary>
    /// <returns></returns>
    public string ToVhdl()
    {
        StringBuilder sb = new();

        // Header
        sb.AppendLine("library ieee");
        sb.AppendLine("use ieee.std_logic_1164.all;\n");

        // Submodules
        foreach (var module in ModulesUsed)
        {
            sb.AppendLine(module.ToVhdlInner());
            sb.AppendLine();
        }

        // Main module
        sb.AppendLine(ToVhdlInner());

        return sb.ToString();
    }

    /// <summary>
    /// Function that only generates this module without submodules or 
    /// stuff that goes at the beginning of the file
    /// </summary>
    /// <returns></returns>
    private string ToVhdlInner()
    {
        if (!Complete)
            throw new Exception("Module not yet complete");

        StringBuilder sb = new();

        // Entity statement
        sb.AppendLine($"entity {Name} is");
        sb.AppendLine("\tport (");
        sb.AppendJoin(";\n", Ports.Select(p => p.ToVhdl.AddIndentation(2)));
        sb.AppendLine();
        sb.AppendLine(");".AddIndentation(1));
        sb.AppendLine($"end {Name};");

        // Architecture
        sb.AppendLine();
        sb.AppendLine($"architecture rtl of {Name} is");

        // Signals
        foreach(NamedSignal signal in NamedSignals.Except(Ports.Select(p => p.Signal)))
        {
            sb.AppendLine($"signal {signal.ToVhdl}".AddIndentation(1));
        }

        // Begin
        sb.AppendLine("begin");

        // Behaviors
        foreach ((NamedSignal outputSignal, DigitalBehavior behavior) in SignalBehaviors)
        {
            sb.AppendLine(behavior.ToVhdl(outputSignal).AddIndentation(1));
        }

        // End
        sb.AppendLine("end rtl;");

        return sb.ToString();
    }

    /// <summary>
    /// Convert module to spice subcircuit
    /// </summary>
    /// <param name="subcircuit">Whether it should be wrapped in a subcircuit or top-level</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string ToSpice(bool subcircuit = false)
    {
        if (!Complete)
            throw new Exception("Module not yet complete");

        StringBuilder sb = new();
        
        // Start subcircuit
        if (subcircuit)
            sb.AppendLine($".subckt {Name} {string.Join(' ', PortsToSpice())}\n");

        int indentation = subcircuit ? 1 : 0;

        // Add all inner modules' subcircuit declarations
        foreach (Module submodule in Instantiations.Select(i => i.InstantiatedModule).Distinct())
            sb.AppendLine(submodule.ToSpice(true).AddIndentation(indentation) + "\n");

        // Add VDD node and PMOS/NMOS models
        sb.AppendLine($"V_VDD VDD 0 {Util.VDD}".AddIndentation(indentation));
        sb.AppendLine($".MODEL {Util.NmosModelName} NMOS".AddIndentation(indentation));
        sb.AppendLine($".MODEL {Util.PmosModelName} PMOS".AddIndentation(indentation));

        // Add all instantiations
        foreach (Instantiation instantiation in Instantiations)
            sb.AppendLine(instantiation.ToSpice().AddIndentation(indentation));
        sb.AppendLine();

        // Add behaviors
        int i = 0;
        foreach ((NamedSignal signal, DigitalBehavior behavior) in SignalBehaviors)
            sb.AppendLine(behavior.ToSpice(signal, i++.ToString()).AddIndentation(indentation));
        
        // Add large resistors from output/bidirectional ports to ground
        foreach (NamedSignal signal in Ports.Where(p => p.Direction == PortDirection.Output || p.Direction == PortDirection.Bidirectional).Select(p => p.Signal))
        {
            int j = 0;
            foreach (SingleNodeNamedSignal singleNodeSignal in signal.ToSingleNodeSignals)
                sb.AppendLine($"R{Util.GetSpiceName(i++.ToString(), j++, "floating")} {singleNodeSignal.ToSpice()} 0 1e6");
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
    public IEnumerable<string> PortsToSpice() => Ports.SelectMany(p => p.Signal.ToSingleNodeSignals).Select(s => s.ToSpice());

    /// <summary>
    /// Convert module to Spice# <see cref="SubcircuitDefinition"/> object
    /// </summary>
    /// <returns></returns>
    public SubcircuitDefinition ToSpiceSharpSubcircuit()
    {
        if (!Complete)
            throw new Exception("Module not yet complete");

        EntityCollection entities = [];
        string[] pins = [.. Ports.SelectMany(p => p.Signal.ToSingleNodeSignals).Select(s => s.ToSpice())];

        // Add VDD node and PMOS/NMOS models
        entities.Add(new VoltageSource("V_VDD", "VDD", "0", Util.VDD));
        Mosfet1Model nmosModel = new(Util.NmosModelName);
        nmosModel.Parameters.SetNmos(true);
        Mosfet1Model pmosModel = new(Util.PmosModelName);
        pmosModel.Parameters.SetPmos(true);
        entities.Add(nmosModel);
        entities.Add(pmosModel);

        // Add instantiations
        foreach (IEntity entity in Instantiation.GetSpiceSharpEntities(Instantiations))
            entities.Add(entity);

        // Add behaviors
        int i = 0;
        foreach ((NamedSignal signal, DigitalBehavior behavior) in SignalBehaviors)
        {
            foreach (IEntity entity in behavior.GetSpiceSharpEntities(signal, i.ToString()))
                entities.Add(entity);
        }
        
        // Add large resistors from output/bidirectional ports to ground
        foreach (NamedSignal signal in Ports.Where(p => p.Direction == PortDirection.Output || p.Direction == PortDirection.Bidirectional).Select(p => p.Signal))
        {
            int j = 0;
            foreach (SingleNodeNamedSignal singleNodeSignal in signal.ToSingleNodeSignals)
                entities.Add(new Resistor($"R{Util.GetSpiceName(i++.ToString(), j++, "floating")}", singleNodeSignal.ToSpice(), "0", 1e6));
        }

        return new(entities, pins);
    }

    /// <summary>
    /// Convert module to Spice# <see cref="Circuit"/> object
    /// </summary>
    /// <returns></returns>
    public Circuit ToSpiceSharpCircuit() => [.. ToSpiceSharpSubcircuit().Entities];
}
