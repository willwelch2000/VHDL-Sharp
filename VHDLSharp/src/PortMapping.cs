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
public class PortMapping : IDictionary<Port, NamedSignal>
{
    private readonly Module module;

    private readonly Module parentModule;
    
    private readonly Dictionary<Port, NamedSignal> backendDictionary = [];

    private ICollection<KeyValuePair<Port, NamedSignal>> BackendDictionaryAsCollection => backendDictionary;

    /// <summary>
    /// Construct port mapping given instantiated module and parent module
    /// </summary>
    /// <param name="module">module that is instantiated</param>
    /// <param name="parentModule">module that contains instantiated module</param>
    public PortMapping(Module module, Module parentModule)
    {
        this.module = module;
        this.module.ModuleUpdated += ModuleUpdated;
        this.parentModule = parentModule;
    }

    /// <summary>
    /// Get all ports that need assignment
    /// </summary>
    public IEnumerable<Port> PortsToAssign => module.Ports.Except(Keys);

    /// <inheritdoc/>
    public ICollection<Port> Keys => backendDictionary.Keys;

    /// <inheritdoc/>
    public ICollection<NamedSignal> Values => backendDictionary.Values;

    /// <inheritdoc/>
    public int Count => backendDictionary.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <summary>
    /// Indexer for port mapping
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public NamedSignal this[Port port]
    {
        get => backendDictionary[port];
        set
        {
            backendDictionary[port] = value;
            CheckValid();
        }
    }

    private void ModuleUpdated(object? sender, EventArgs eventArgs)
    {
        CheckValid();
    }

    private void CheckValid()
    {
        foreach ((Port port, NamedSignal signal) in backendDictionary)
        {
            if (port.Signal.Dimension != signal.Dimension)
                throw new PortMappingException($"Port {port} and signal {signal} must have the same dimension");
            if (port.Signal.ParentModule != module)
                throw new PortMappingException($"Ports must have the specified module {module} as parent");
            if (!module.Ports.Contains(port))
                throw new PortMappingException($"Port {port} must be in the list of ports of specified module {module}");
            if (signal.ParentModule != parentModule)
                throw new PortMappingException($"Signal must have module {parentModule} as parent");
        }
    }

    /// <summary>
    /// True if port mapping is complete (all ports are assigned)
    /// </summary>
    /// <returns></returns>
    public bool Complete() => module.Ports.All(backendDictionary.ContainsKey);

    /// <inheritdoc/>
    public void Add(Port port, NamedSignal signal)
    {
        backendDictionary.Add(port, signal);
        CheckValid();
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<Port, NamedSignal> item) => Add(item.Key, item.Value);

    /// <inheritdoc/>
    public bool ContainsKey(Port port) => backendDictionary.ContainsKey(port);

    /// <inheritdoc/>
    public bool Remove(Port port) => backendDictionary.Remove(port);

    /// <inheritdoc/>
    public bool TryGetValue(Port port, [MaybeNullWhen(false)] out NamedSignal signal) =>
        backendDictionary.TryGetValue(port, out signal);

    /// <inheritdoc/>
    public void Clear() => backendDictionary.Clear();

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<Port, NamedSignal> item) => backendDictionary.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<Port, NamedSignal>[] array, int arrayIndex) => BackendDictionaryAsCollection.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<Port, NamedSignal> item) => BackendDictionaryAsCollection.Remove(item);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<Port, NamedSignal>> GetEnumerator() => backendDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        IEnumerable enumerable = backendDictionary;
        return enumerable.GetEnumerator();
    }
}