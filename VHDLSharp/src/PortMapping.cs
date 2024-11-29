using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace VHDLSharp;

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
/// Mapping of ports of a module to the signals its connected to in an instantiation
/// </summary>
public class PortMapping : IDictionary<Port, ISignal>
{
    private readonly Module module;

    private readonly Module parent;
    
    private readonly Dictionary<Port, ISignal> backendDictionary = [];

    private ICollection<KeyValuePair<Port, ISignal>> BackendDictionaryAsCollection => backendDictionary;

    /// <summary>
    /// Construct port mapping given instantiated module and parent module
    /// </summary>
    /// <param name="module">module that is instantiated</param>
    /// <param name="parent">module that contains instantiated module</param>
    public PortMapping(Module module, Module parent)
    {
        this.module = module;
        this.module.ModuleUpdated += ModuleUpdated;
        this.parent = parent;
    }

    /// <summary>
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<Port> PortsToAssign => module.Ports.Except(Keys);

    /// <inheritdoc/>
    public ICollection<Port> Keys => backendDictionary.Keys;

    /// <inheritdoc/>
    public ICollection<ISignal> Values => backendDictionary.Values;

    /// <inheritdoc/>
    public int Count => backendDictionary.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <summary>
    /// Indexer for port mapping
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public ISignal this[Port port]
    {
        get => backendDictionary[port];
        set => Add(port, value);
    }

    private void ModuleUpdated(object? sender, EventArgs eventArgs)
    {
        CheckValid();
    }

    private void CheckValid()
    {
        foreach ((Port port, ISignal signal) in backendDictionary)
        {
            if (port.Signal.Dimension != signal.Dimension)
                throw new PortMappingException($"Port {port} and signal {signal} must have the same dimension");
            if (port.Signal.Parent != module)
                throw new PortMappingException($"Ports must have the specified module {module} as parent");
            if (!module.Ports.Contains(port))
                throw new PortMappingException($"Port {port} must be in the list of ports of specified module {module}");
            if (signal.Parent != parent)
                throw new PortMappingException($"Signal must have module {parent} as parent");
        }
    }

    /// <summary>
    /// True if port mapping is complete (all ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool Complete() => module.Ports.All(backendDictionary.ContainsKey);

    /// <inheritdoc/>
    public void Add(Port port, ISignal signal)
    {
        backendDictionary.Add(port, signal);
        CheckValid();
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<Port, ISignal> item) => Add(item.Key, item.Value);

    /// <inheritdoc/>
    public bool ContainsKey(Port port) => backendDictionary.ContainsKey(port);

    /// <inheritdoc/>
    public bool Remove(Port port) => backendDictionary.Remove(port);

    /// <inheritdoc/>
    public bool TryGetValue(Port port, [MaybeNullWhen(false)] out ISignal signal) =>
        backendDictionary.TryGetValue(port, out signal);

    /// <inheritdoc/>
    public void Clear() => backendDictionary.Clear();

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<Port, ISignal> item) => backendDictionary.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<Port, ISignal>[] array, int arrayIndex) => BackendDictionaryAsCollection.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<Port, ISignal> item) => BackendDictionaryAsCollection.Remove(item);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<Port, ISignal>> GetEnumerator() => backendDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        IEnumerable enumerable = backendDictionary;
        return enumerable.GetEnumerator();
    }
}