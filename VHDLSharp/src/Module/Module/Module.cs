using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Algorithms;
using VHDLSharp.Behaviors;
using VHDLSharp.Exceptions;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
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

    // Used as validity checker, so it should be set false before control leaves the module
    // It is possible for there to be compiled objects even if this is false
    private bool compiled;

    private readonly HashSet<IDerivedSignal> registeredDerivedSignals = [];

    /// <summary>Default constructor</summary>
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
    /// Get all module-specific signals used here, potentially even those that belong to another module.
    /// This only happens if there's an error, so it is used to check if they were assigned by error
    /// </summary>
    /// <param name="includeIncorrectlyAssignedSignals">If true, includes module-specific signals used here even if they belong to another module</param>
    /// <returns></returns>
    private HashSet<IModuleSpecificSignal> GetAllModuleSignals(bool includeIncorrectlyAssignedSignals)
    {
        // Get list of all single-node named signals used
        HashSet<ISingleNodeModuleSpecificSignal> allSingleNodeSignals =
            [.. Ports.Select(p => p.Signal) // Port signals
            .Union(SignalBehaviors.Values.SelectMany(b => b.InputModuleSignals)) // Behaviors' input signals
            .Union(SignalBehaviors.Keys) // Assigned signals
            .Union(Instantiations.SelectMany(i => i.PortMapping.Values)) // Signals used in instantations
            .Union(registeredDerivedSignals) // Registered derived signals
            // From all included signals, get themselves and any used signals + linked signal, if it's a derived signal or derived signal node
            .SelectMany<IModuleSpecificSignal, IModuleSpecificSignal>(s => s switch
            {
                IDerivedSignal derivedSignal => derivedSignal.LinkedSignal is INamedSignal linkedSignal ?
                    [s, .. derivedSignal.RecursiveInputModuleSignals, linkedSignal] : [s, .. derivedSignal.RecursiveInputModuleSignals],
                IDerivedSignalNode derivedSignalNode => derivedSignalNode.DerivedSignal.LinkedSignal is INamedSignal linkedSignal ?
                    [s, .. derivedSignalNode.DerivedSignal.RecursiveInputModuleSignals, linkedSignal] : [s, .. derivedSignalNode.DerivedSignal.RecursiveInputModuleSignals],
                _ => [s],
            })
            .Where(s => s.ParentModule == this || includeIncorrectlyAssignedSignals) // Should be true for all, but double check
            .SelectMany(s => s.ToSingleNodeSignals)]; // Convert to the single-node signals

        HashSet<IModuleSpecificSignal> topLevelSignals = [.. allSingleNodeSignals.Select(s => s.TopLevelSignal)];
        HashSet<IModuleSpecificSignal> allModuleSignals = [];

        foreach (IModuleSpecificSignal topLevelSignal in topLevelSignals)
        {
            // If all child signals are present, add top level one
            if (topLevelSignal.ToSingleNodeSignals.All(allSingleNodeSignals.Contains))
                allModuleSignals.Add(topLevelSignal);
            else
                // Otherwise, add single-node signals that are present
                foreach (ISingleNodeModuleSpecificSignal singleNodeSignal in topLevelSignal.ToSingleNodeSignals.Where(allSingleNodeSignals.Contains))
                    allModuleSignals.Add(singleNodeSignal);
        }

        return allModuleSignals;
    }

    /// <inheritdoc/>
    public IEnumerable<IModuleSpecificSignal> AllModuleSignals => GetAllModuleSignals(false);

    /// <inheritdoc/>
    public IEnumerable<IDerivedSignal> AllDerivedSignals => [.. registeredDerivedSignals];

    /// <inheritdoc/>
    public ISet<IModule> GetModulesUsed(bool recursive, bool compileDerivedSignals)
    {
        HashSet<IModule> modulesUsed = [];
        bool uncompile = false;
        if (compileDerivedSignals)
            uncompile = CompileDerivedSignals(); // undo compilation if it actually changes stuff
        foreach (IModule module in Instantiations.Select(i => i.InstantiatedModule))
        {
            if (!modulesUsed.Add(module))
                continue;
            // Add module's used modules if recursive and we haven't already added this module
            if (recursive)
                foreach (IModule submodule in module.GetModulesUsed(recursive, compileDerivedSignals))
                    modulesUsed.Add(submodule);
        }
        if (uncompile)
            compiled = false;
        return modulesUsed;
    }

    private bool ConsiderValid => ignoreValidity || ValidityManager.IsValid(out _);

    /// <inheritdoc/>
    public IEnumerable<IMayBeRecursive<IModule>> Children => GetModulesUsed(false, false);

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
        ITopLevelNamedSignal signal = dimension == 1 ? new Signal(name, this) : new Vector(name, this, dimension);
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
    public Port AddNewPort(ITopLevelNamedSignal signal, PortDirection direction)
    {
        if (!((IModule)this).Equals(signal.ParentModule))
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
    public Instantiation AddNewInstantiation(IModule module, string name) => Instantiations.Add(module, name);

    /// <summary>
    /// Add new instantiation automatically using this module as parent module. 
    /// Connections are also provided and assigned to the instantiation's ports in the same order
    /// </summary>
    /// <param name="module">Module to be instantiated in this</param>
    /// <param name="name">Name of instantiation</param>
    /// <param name="connections">Signals to be assigned to the module's ports</param>
    /// <returns></returns>
    public Instantiation AddNewInstantiation(IModule module, string name, params INamedSignal[] connections)
    {
        Instantiation inst = AddNewInstantiation(module, name);
        IPort[] ports = [.. module.Ports];
        if (ports.Length != connections.Length)
            throw new Exception($"Wrong number of ports. Must have {ports.Length}.");
        for (int i = 0; i < ports.Length; i++)
            inst.PortMapping[ports[i]] = connections[i];
        return inst;
    }

    // TODO if I keep this structure where a signal can have > 2 levels of hierarchy, needs to be changed
    /// <inheritdoc/>
    public bool IsComplete([MaybeNullWhen(true)] out string reason)
    {
        // Set of all assigned output signals, broken into their single-node components
        HashSet<ISingleNodeNamedSignal> allAssignedOutputSignals = [.. Instantiations.GetSignals(PortDirection.Output)
            .Concat(SignalBehaviors.Keys)
            .Concat(AllModuleSignals.OfType<IDerivedSignal>().Select(s => s.LinkedSignal).OfType<INamedSignal>())
            .SelectMany(s => s.ToSingleNodeSignals)];

        // Check that every single-node signal in every port has been assigned
        // This strategy should work for a Vector assigned as multiple VectorSlices
        foreach (IPort port in Ports.Where(p => p.Direction == PortDirection.Output))
            if (!port.Signal.ToSingleNodeSignals.All(allAssignedOutputSignals.Contains))
            {
                reason = $"Port {port} has not been assigned";
                return false;
            }

        // Check that instantiation collection is complete
        if (!Instantiations.IsComplete(out reason))
            return false;

        // Check all applicable behaviors
        foreach (ICompletable behavior in SignalBehaviors.Values.OfType<ICompletable>())
            if (!behavior.IsComplete(out reason))
                return false;

        reason = null;
        return true;
    }

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
        if (!ConsiderValid)
            throw new InvalidException("Module is invalid", ValidityManager.Issues().First().Exception);

        if (!IsComplete(out string? reason))
            throw new IncompleteException($"Module not yet complete: {reason}");

        StringBuilder sb = new();

        // Header
        sb.AppendLine("library ieee");
        sb.AppendLine("use ieee.std_logic_1164.all;\n");

        // Subcircuits and this already checked
        ignoreValidity = true;

        // Main module
        sb.AppendLine(GetVhdlNoSubmodules());

        // Submodules--need to do all modules recursively
        bool firstLoop = true;
        foreach (IModule module in GetModulesUsed(true, true))
        {
            if (!firstLoop)
            {
                firstLoop = false;
                sb.AppendLine();
            }
            sb.AppendLine(module.GetVhdlNoSubmodules());
        }

        ignoreValidity = false;
        return sb.ToString().TrimEnd();
    }

    /// <inheritdoc/>
    public string GetVhdlNoSubmodules()
    {
        if (!ConsiderValid)
            throw new InvalidException("Module is invalid", ValidityManager.Issues().First().Exception);

        if (!IsComplete(out string? reason))
            throw new IncompleteException($"Module not yet complete: {reason}");

        CompileDerivedSignals();

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
        IModuleSpecificSignal[] moduleSignals = [.. AllModuleSignals];
        foreach (INamedSignal signal in moduleSignals.OfType<INamedSignal>()
            .Union(moduleSignals.OfType<IDerivedSignal>().Select(d => d.LinkedSignal ??
                throw new Exception("Derived signal must be linked to named signal before getting VHDL")))
            .Except(Ports.Select(p => p.Signal)))
        {
            sb.AppendLine(signal.GetVhdlDeclaration().AddIndentation(1));
        }

        // Component declarations--don't need recursive because it should just declare the ones it directly uses
        bool firstLoop = true;
        foreach (IModule module in GetModulesUsed(false, false))
        {
            if (firstLoop)
            {
                firstLoop = false;
                sb.AppendLine();
            }
            sb.AppendLine(module.GetVhdlComponentDeclaration().AddIndentation(1));
        }

        // Begin
        sb.AppendLine("begin");

        // Add all instantiations
        if (Instantiations.Count != 0)
        {
            sb.Append(Instantiations.GetVhdl().AddIndentation(1));
            sb.AppendLine();
        }

        // Behaviors
        foreach ((INamedSignal outputSignal, IBehavior behavior) in SignalBehaviors)
        {
            sb.AppendLine(behavior.GetVhdlStatement(outputSignal).AddIndentation(1));
        }

        // End
        sb.AppendLine("end rtl;");

        compiled = false;
        return sb.ToString();
    }

    /// <inheritdoc/>
    public SpiceSubcircuit GetSpice() => GetSpice(new HashSet<IModuleLinkedSubcircuitDefinition>());

    /// <inheritdoc/>
    public SpiceSubcircuit GetSpice(ISet<IModuleLinkedSubcircuitDefinition> existingModuleLinkedSubcircuits)
    {
        if (!ConsiderValid)
            throw new InvalidException("Module is invalid", ValidityManager.Issues().First().Exception);

        if (!IsComplete(out string? reason))
            throw new IncompleteException($"Module not yet complete: {reason}");

        CompileDerivedSignals();

        string[] pins = [.. PortsToSpice()];

        // Initialize collection of additional entities to add
        EntityCollection additionalEntities = [];

        // List of circuits that will be combined
        List<SpiceCircuit> circuits = [];

        // Add instantiations
        ignoreValidity = true; // Subcircuits already checked
        circuits.Add(Instantiations.GetSpice(existingModuleLinkedSubcircuits));
        ignoreValidity = false;

        // Add behaviors
        int i = 0;
        foreach ((INamedSignal signal, IBehavior behavior) in SignalBehaviors)
            circuits.Add(behavior.GetSpice(signal, i++.ToString()));

        // Add large resistors from output/bidirectional ports to ground
        // foreach (INamedSignal signal in Ports.Where(p => p.Direction == PortDirection.Output || p.Direction == PortDirection.Bidirectional).Select(p => p.Signal)) TODO
        foreach (INamedSignal signal in Ports.Where(p => p.Direction == PortDirection.Output).Select(p => p.Signal))
        {
            int j = 0;
            foreach (ISingleNodeNamedSignal singleNodeSignal in signal.ToSingleNodeSignals)
                additionalEntities.Add(new Resistor(SpiceUtil.GetSpiceName(i++.ToString(), j++, "floating"), singleNodeSignal.GetSpiceName(), "0", 1e9));
        }

        circuits.Add(new(additionalEntities));
        SpiceSubcircuit subcircuit = SpiceCircuit.Combine(circuits).WithCommonEntities().ToSpiceSubcircuit(this, pins);

        compiled = false;
        return subcircuit;
    }

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules() => GetSimulationRules(new(this, []));

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules(SubcircuitReference subcircuit)
    {
        if (!ConsiderValid)
            throw new InvalidException("Module is invalid", ValidityManager.Issues().First().Exception);
        if (!IsComplete(out string? reason))
            throw new IncompleteException($"Module not yet complete: {reason}");
        if (!((IValidityManagedEntity)subcircuit).ValidityManager.IsValid(out Exception? issue))
            throw new InvalidException("Subcircuit reference must be valid to use to get simulation rule", issue);
        if (!((IModule)this).Equals(subcircuit.FinalModule))
            throw new Exception($"The provided subcircuit reference must reference this ({ToString()}), not {subcircuit.FinalModule.ToString()}");

        CompileDerivedSignals();

        // Behaviors
        foreach ((INamedSignal signal, IBehavior behavior) in SignalBehaviors)
            yield return behavior.GetSimulationRule(subcircuit.GetChildSignalReference(signal));

        // Instantiations
        foreach (SimulationRule rule in Instantiations.SelectMany(i => i.InstantiatedModule.GetSimulationRules(subcircuit.GetChildSubcircuitReference(i))))
            yield return rule;

        compiled = false;
    }

    /// <summary>
    /// All ports converted to Spice strings
    /// </summary>
    /// <returns></returns>
    private IEnumerable<string> PortsToSpice() => Ports.SelectMany(p => p.Signal.ToSingleNodeSignals).Select(s => s.GetSpiceName());

    /// <summary>
    /// Test if the module contains a signal
    /// TODO if I keep this structure where a signal can have > 2 levels of hierarchy, needs to be changed
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public bool ContainsSignal(IModuleSpecificSignal signal)
    {
        IModuleSpecificSignal[] namedSignals = [.. AllModuleSignals];
        // True if directly contained
        if (namedSignals.Contains(signal))
            return true;
        // True if signal is single-node and the parent is contained
        return signal is ISingleNodeModuleSpecificSignal && namedSignals.Contains(signal.TopLevelSignal);
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

        // Check for recursion
        if (((IMayBeRecursive<IModule>)this).CheckRecursion())
            exception = new IllegalRecursionException("Recursion detected in module usage");

        // Check that all module signals have this module as parent
        IModuleSpecificSignal[] moduleSignals = [.. GetAllModuleSignals(true)];
        foreach (IModuleSpecificSignal moduleSignal in moduleSignals)
            if (!((IModule)this).Equals(moduleSignal.ParentModule))
                exception = moduleSignal is INamedSignal namedModSignal ?
                    new Exception($"Signal {namedModSignal.Name} must have this module ({Name}) as parent") :
                    new Exception($"Signals must have this module ({Name}) as parent");

        // Check that behaviors have correct dimension and that output signal isn't input port
        INamedSignal[] inputPortSignals = [.. Ports.Where(p => p.Direction == PortDirection.Input).Select(p => p.Signal)];
        foreach ((INamedSignal outputSignal, IBehavior behavior) in SignalBehaviors)
        {
            if (behavior.ParentModule is not null && !((IModule)this).Equals(behavior.ParentModule))
                exception = new Exception($"Behavior must have this module as parent");
            if (!behavior.IsCompatible(outputSignal))
                exception = new Exception($"Behavior must be compatible with output signal");
            if (inputPortSignals.Contains(outputSignal))
                exception = new Exception($"Output signal ({outputSignal}) must not be an input port");
        }

        // This list removes duplicate derived signals (from when a node references a parent),
        // but keeps duplicate linked signals that come from different derived signals--not allowed, checked below
        List<INamedSignal> linkedSignals = [];

        // Check that linked signals have the right dimension and aren't input ports
        foreach (IDerivedSignal derivedSignal in moduleSignals.OfType<IDerivedSignal>()
            .Union(moduleSignals.OfType<IDerivedSignalNode>().Select(s => s.DerivedSignal)))
        {
            if (derivedSignal.LinkedSignal is not null)
            {
                INamedSignal linkedSignal = derivedSignal.LinkedSignal;
                linkedSignals.Add(linkedSignal);
                if (!derivedSignal.Dimension.Compatible(linkedSignal.Dimension))
                    exception = new Exception($"Linked signal ({linkedSignal}) must have the same dimension as the derived signal it's linked to");
                if (inputPortSignals.Contains(linkedSignal))
                    exception = new Exception($"Linked signal ({linkedSignal}) must not be an input port");
            }
        }

        // Throw error if a signal has two or more assignments--either from behavior mapping, output port of (non-compiled) instantiation, or derived signal linked signal
        List<ISingleNodeNamedSignal> allSingleNodeOutputSignals = [.. SignalBehaviors.Keys
            .Concat(Instantiations.Where(i => i is not ICompiledObject) // Ignore instantiations added due to compilation
                .SelectMany(i => i.GetSignals(PortDirection.Output)))
            .Concat(linkedSignals)
            .SelectMany(s => s.ToSingleNodeSignals)];
        if (allSingleNodeOutputSignals.Count != allSingleNodeOutputSignals.Distinct().Count())
        {
            ISingleNodeNamedSignal child = allSingleNodeOutputSignals.First(s => allSingleNodeOutputSignals.Count(s2 => s2 == s) > 1);
            INamedSignal? parent = child.ParentSignal;
            if (parent is not null)
                exception = new Exception($"Module defines an overlapping parent ({parent}) and child ({child}) output signal");
            else
                exception = new Exception($"Module has two declarations for a signal ({child}) either as behavior, instantiation output, or as a used derived signal's linked signal");
        }

        // Don't allow ports with the same signal
        HashSet<ISignal> portSignals = [];
        if (!Ports.All(p => portSignals.Add(p.Signal)))
        {
            string? duplicate = Ports.FirstOrDefault(p => Ports.Count(p2 => p.Signal == p2.Signal) > 1)?.Signal?.Name;
            if (duplicate is not null)
                exception = new Exception($"The same signal (\"{duplicate}\") cannot be added as two different ports");
        }

        // Check circular signal assignment
        if (ModuleAlgorithms.CheckForCircularSignals(this, out List<List<IModuleSpecificSignal>> paths))
            exception = new CircularSignalException($"Circular signal path detected: {string.Join('-', paths.First())}");

        return exception is null;
    }

    /// <inheritdoc/>
    public bool Equals(IModule? other) => other is not null && other.BaseModule == this;

    /// <summary>
    /// Generate instantiations and linked signals for derived signals
    /// </summary>
    /// <returns>True if compilation is performed, false if not necessary because it already has been compiled</returns>
    private bool CompileDerivedSignals()
    {
        if (compiled)
            return false;

        // Remove all compiled instantiations--signals are removed in the section below
        foreach (CompiledInstantiation instantiation in Instantiations.OfType<CompiledInstantiation>().ToArray())
            Instantiations.Remove(instantiation);

        // Gather all used derived signals and the derived signals whose nodes are used
        IModuleSpecificSignal[] moduleSignals = [.. AllModuleSignals];
        IDerivedSignal[] usedDerivedSignals = [.. moduleSignals.OfType<IDerivedSignal>().Distinct()
            .Union(moduleSignals.OfType<IDerivedSignalNode>().Select(s => s.DerivedSignal))];
        // Loop through the derived signals--if the signal is unlinked or previously compiled, link a new named signal
        foreach ((int i, IDerivedSignal derivedSignal) in usedDerivedSignals.Index())
            if (derivedSignal.LinkedSignal is ICompiledObject or null)
                derivedSignal.LinkedSignal = derivedSignal.Dimension.NonNullValue switch
                {
                    1 => new CompiledSignal($"DerivedSignal{i}", this),
                    _ => new CompiledVector($"DerivedSignal{i}", this, derivedSignal.Dimension.NonNullValue),
                };
        // Loop through again and add the compiled instantiation, converting to a CompiledInstantiation instance
        // These loops cannot be combined because cometimes the instantiation compilation requires another linked signal existing already
        foreach ((int i, IDerivedSignal derivedSignal) in usedDerivedSignals.Index())
            Instantiations.Add(new CompiledInstantiation(derivedSignal.Compile($"DerivedModule{i}", $"DerivedInstance{i}")));

        compiled = true;
        return true;
    }

    /// <inheritdoc/>
    public void UndoDerivedSignalCompilation()
    {
        // Remove all instantiations
        foreach (IInstantiation instantiation in Instantiations.OfType<CompiledInstantiation>())
            Instantiations.Remove(instantiation);
        // Remove all auto-linked signals
        foreach (IDerivedSignal derivedSignal in AllModuleSignals.OfType<IDerivedSignal>().Where(s => s.LinkedSignal is ICompiledObject))
            derivedSignal.LinkedSignal = null;
        compiled = false;
    }

    /// <inheritdoc/>
    public void RegisterDerivedSignal(IDerivedSignal signal)
    {
        if (!signal.ParentModule.Equals(this))
            throw new Exception($"The provided derived signal does must have this ({Name}) as its parent module");
        registeredDerivedSignals.Add(signal);
    }
}
