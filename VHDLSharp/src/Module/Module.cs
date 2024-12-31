using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using VHDLSharp.Behaviors;
using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Modules;

/// <summary>
/// A digital module--a circuit that has some functionality
/// </summary>
public class Module
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public Module()
    {
        Ports.CollectionChanged += InvokeModuleUpdated;
        SignalBehaviors.CollectionChanged += InvokeModuleUpdated;
        SignalBehaviors.CollectionChanged += BehaviorsListUpdated;
        Instantiations.CollectionChanged += InvokeModuleUpdated;
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
        ModuleUpdated?.Invoke(this, e);
    }

    private void BehaviorsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (object newItem in e.NewItems)
                if (newItem is (NamedSignal outputSignal, CombinationalBehavior behavior))
                    behavior.BehaviorUpdated += InvokeModuleUpdated;
    }


    /// <summary>
    /// Event called when a property of the module is changed that could affect other objects,
    /// such as port mapping
    /// </summary>
    public event EventHandler? ModuleUpdated;

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
        .Union(SignalBehaviors.Keys);

    /// <summary>
    /// Get all modules (recursive) used by this module as instantiations
    /// </summary>
    public IEnumerable<Module> ModulesUsed =>
        Instantiations.SelectMany(i => i.Module.ModulesUsed.Append(i.Module)).Distinct();

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
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;

    private void CheckValid()
    {
        // Check that behaviors are in correct module/have correct dimension
        foreach ((NamedSignal outputSignal, DigitalBehavior behavior) in SignalBehaviors)
        {
            if (outputSignal.ParentModule != this)
                throw new Exception($"Output signal {outputSignal.Name} must have this module ({Name}) as parent");
            if (behavior.ParentModule is not null && behavior.ParentModule != this)
                throw new Exception($"Behavior must have this module as parent");
            if (!behavior.Dimension.Compatible(outputSignal.Dimension))
                throw new Exception("Behavior must have same dimension as assigned output signal");
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
}
