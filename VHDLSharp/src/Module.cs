using System.Collections.ObjectModel;

namespace VHDLSharp;

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
        Ports.CollectionChanged += PortsUpdated;
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

    private void PortsUpdated(object? sender, EventArgs e)
    {
        ModuleUpdated?.Invoke(this, e);
    }


    /// <summary>
    /// Event called when a property of the module is changed that could affect other objects
    /// </summary>
    public event EventHandler? ModuleUpdated;

    /// <summary>
    /// Name of the module
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// All behaviors that define the module
    /// </summary>
    public List<IDigitalBehavior> Behaviors { get; set; } = [];

    /// <summary>
    /// Mapping of events to the actions that happen with that event
    /// </summary>
    public List<(IDigitalEvent Event, IEnumerable<IDigitalAction> Actions)> EventMappings { get; set; } = [];

    /// <summary>
    /// List of ports for this module
    /// </summary>
    public ObservableCollection<Port> Ports { get; } = [];

    /// <summary>
    /// List of module instantiations inside of this module
    /// </summary>
    public List<Instantiation> Instantiations { get; set; } = [];

    

    /// <summary>
    /// Get all signals used in this module
    /// Signals can come from ports, behaviors, or event mappings' actions
    /// </summary>
    public IEnumerable<Signal> Signals =>
        Ports.Select(p => p.Signal)
        .Union(Behaviors.SelectMany(b => b.InvolvedSignals))
        .Union(EventMappings.SelectMany(e => e.Actions).SelectMany(a => a.InvolvedSignals));

    /// <summary>
    /// Get all modules used by this module as instantiations
    /// </summary>
    public IEnumerable<Module> ModulesUsed =>
        Instantiations.Select(i => i.Module).Distinct();

    /// <summary>
    /// Generate a signal with this module as the parent
    /// </summary>
    /// <param name="name">name of the signal</param>
    /// <returns></returns>
    public Signal GenerateSignal(string name) => new(name, this);

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
            Signal = new(name, this),
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
    public Port AddNewPort(Signal signal, PortDirection direction)
    {
        if (signal.Parent != this)
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
}
