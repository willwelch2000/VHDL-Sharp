using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.Algorithms;

/// <summary>
/// Algorithms used for modules
/// </summary>
public static class ModuleAlgorithms
{
    /// <summary>
    /// Checks a module for circular signal assignment, where a signal is a (direct or indirect) input signal of itself
    /// </summary>
    /// <param name="module"></param>
    /// <param name="paths">Paths of signals that form a circle</param>
    /// <returns></returns>
    public static bool CheckForCircularSignals(IModule module, out List<List<IModuleSpecificSignal>> paths)
    {
        // Find all input-output connections--dictionary maps signal to its immediate inputs
        // Assumes that all inputs to an instance are inputs to all of its outputs
        var mapping = GetSignalMapping(module, out var connectionsThroughInstantiation);

        // Check if there are circular paths
        bool circular = CheckForCircularity(mapping, out paths, findAllPaths: true);
        if (!circular)
            return false;

        // In future version, first check if there are paths that don't go through instances
        List<List<IModuleSpecificSignal>> connectedPaths = [];
        Dictionary<IPort, Dictionary<IPort, bool>> cache = [];
        // See which of the paths are actually connected through instances
        foreach (var path in paths)
        {
            bool connected = true;
            foreach ((IModuleSpecificSignal signal1, IModuleSpecificSignal signal2) in GetPairsInPath(path))
            {
                // Connection in path relies on instance--check it
                if (connectionsThroughInstantiation.TryGetValue((signal1, signal2), out IInstantiation? instantiation))
                {
                    // Find ports in instantiation
                    IPort port1 = instantiation.PortMapping.First(kvp => kvp.Value.Equals(signal1)).Key;
                    IPort port2 = instantiation.PortMapping.First(kvp => kvp.Value.Equals(signal2)).Key;
                    connected = CheckPortConnection(port1, port2, cache);
                }
            }
            if (connected)
                connectedPaths.Add(path);
        }
        paths = connectedPaths;
        return paths.Count > 0;
    }

    /// <summary>
    /// Check if an input and output port are connected
    /// </summary>
    /// <param name="inputPort"></param>
    /// <param name="outputPort"></param>
    /// <param name="cache">
    /// For already-examined module ports, maps output ports to another dictionary
    /// where the keys are all the input ports that are connected, assuming all sub-instance ports are connected. 
    /// The values in that dictionary are whether the connection has been fully determined (through sub-instances).
    /// </param>
    /// <returns></returns>
    public static bool CheckPortConnection(IPort inputPort, IPort outputPort, Dictionary<IPort, Dictionary<IPort, bool>> cache)
    {
        IModule module = inputPort.Signal.ParentModule;
        if (!outputPort.Signal.ParentModule.Equals(module))
            throw new Exception("Module of ports must match");
        if (inputPort.Direction != PortDirection.Input || outputPort.Direction != PortDirection.Output)
            throw new Exception("Incorrect direction for port");

        // Try cache
        if (cache.TryGetValue(outputPort, out var inputs))
        {
            // Definitely not an input
            if (!inputs.TryGetValue(inputPort, out bool explored))
                return false;
            // A fully-explored input
            if (explored)
                return true;
            // Might be an input--just continue on
        }

        // Cache is either nonexistent or port is not fully explored
        bool noCacheYet = inputs is null;
        inputs ??= cache[outputPort] = [];
        Dictionary<IModuleSpecificSignal, IPort> inputSignalToPort = module.Ports.Where(p => p.Direction == PortDirection.Input).Select(p => ((IModuleSpecificSignal)p.Signal, p)).ToDictionary();
        var mapping = GetSignalMapping(module, out var connectionsThroughInstantiation);
        // Go ahead and check connection to all input ports
        CheckForConnections(mapping, [outputPort.Signal], [.. module.Ports.Where(p => p.Direction == PortDirection.Input).Select(p => p.Signal)], out var paths);
        // TODO first check for any path that doesn't go through an instance
        foreach (List<IModuleSpecificSignal> path in paths)
        {
            IPort pathInputPort = inputSignalToPort[path.Last()];
            // Skip if already explored
            if (inputs.TryGetValue(pathInputPort, out bool explored) && explored)
                continue;
            // Fully explore the primary path
            if (pathInputPort.Equals(inputPort))
            {
                bool connected = true;
                foreach ((IModuleSpecificSignal signal1, IModuleSpecificSignal signal2) in GetPairsInPath(path))
                {
                    // Connection in path relies on instance--check it
                    if (connectionsThroughInstantiation.TryGetValue((signal1, signal2), out IInstantiation? instantiation))
                    {
                        // Find ports in instantiation
                        IPort port1 = instantiation.PortMapping.First(kvp => kvp.Value.Equals(signal1)).Key;
                        IPort port2 = instantiation.PortMapping.First(kvp => kvp.Value.Equals(signal2)).Key;
                        connected = CheckPortConnection(port1, port2, cache);
                    }
                    if (!connected)
                        break;
                }
                // Either fully connected (true) or not at all (nothing assigned to dictionary)
                if (connected)
                    inputs[pathInputPort] = true;
                else
                    inputs.Remove(pathInputPort);
            }
            // Secondary path--don't look in instances, but count as explored if there are no instance-reliant connections
            else
            {
                bool usesInstance = false;
                foreach ((IModuleSpecificSignal signal1, IModuleSpecificSignal signal2) in GetPairsInPath(path))
                    if (connectionsThroughInstantiation.ContainsKey((signal1, signal2)))
                        usesInstance = true;
                if (!usesInstance)
                    inputs[pathInputPort] = true;
                // Add as unexplored only if cache didn't already exist--if it did this would be done already
                else if (noCacheYet)
                    inputs[pathInputPort] = false;
                // No option to downgrade with secondary path--it either gets marked/left as false or set to true
            }
        }
        return false;
    }

    /// <summary>
    /// Checks for circular assignment given dictionary mapping something to its one-directional neighbors
    /// https://www.geeksforgeeks.org/dsa/detect-cycle-in-a-graph/
    /// </summary>
    /// <param name="mapping"></param>
    /// <param name="paths">Paths of nodes that form a circle</param>
    /// <param name="findAllPaths">If true, find all circular paths and not just first. If false, only one is found.</param>
    /// <returns></returns>
    public static bool CheckForCircularity<T, V>(Dictionary<T, V> mapping, out List<List<T>> paths, bool findAllPaths = false) where T : notnull where V : IEnumerable<T>
    {
        HashSet<T> visited = [];
        Stack<T> recStack = [];
        List<List<T>> addedPaths = [];
        bool pathFound = false;

        // Considers current recStack and final node
        void AddPath(T finalNode)
        {
            // Last thing added is node that completes the path, so it is the first and last node
            List<T> path = [finalNode];
            bool skip = true;
            foreach (T stackNode in recStack.Reverse())
            {
                if (!skip)
                    path.Add(stackNode);
                if (skip && stackNode.Equals(finalNode))
                    skip = false;
            }
            addedPaths.Add(path);
        }

        bool SearchFirstPath(T node)
        {
            if (recStack.Contains(node))
            {
                AddPath(node);
                return true;
            }
            if (visited.Contains(node))
                return false;
            visited.Add(node);
            recStack.Push(node);
            foreach (T neighbor in mapping.TryGetValue(node, out V? neighbors) ? neighbors.AsEnumerable() : [])
                if (SearchFirstPath(neighbor))
                    return true;
            recStack.Pop();
            return false;
        }

        void Search(T node)
        {
            if (recStack.Contains(node))
            {
                pathFound = true;
                AddPath(node);
            }
            if (visited.Contains(node))
                return;
            visited.Add(node);
            recStack.Push(node);
            foreach (T neighbor in mapping[node])
                Search(neighbor);
            recStack.Pop();
        }

        if (findAllPaths)
            foreach (T node in mapping.Keys)
                Search(node);
        else
            foreach (T node in mapping.Keys)
                if (SearchFirstPath(node))
                {
                    pathFound = true;
                    break;
                }
        paths = addedPaths;
        return pathFound;
    }

    /// <summary>
    /// Does depth-first search to find all paths from start nodes to end nodes.
    /// Algorithm based on <see cref="CheckForCircularity"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <param name="mapping"></param>
    /// <param name="startNodes"></param>
    /// <param name="endNodes"></param>
    /// <param name="paths">Connections from start nodes to end nodes</param>
    /// <returns></returns>
    public static bool CheckForConnections<T, V>(Dictionary<T, V> mapping, List<T> startNodes, HashSet<T> endNodes, out List<List<T>> paths) where T : notnull where V : IEnumerable<T>
    {
        HashSet<T> notInPath = [];
        Stack<T> recStack = [];
        List<List<T>> addedPaths = [];
        bool pathFound = false;

        // Considers current recStack and final node
        void AddPath(T finalNode)
        {
            addedPaths.Add([.. recStack.Reverse(), finalNode]);
            pathFound = true;
        }

        bool Search(T node)
        {
            // Found path
            if (endNodes.Contains(node))
            {
                AddPath(node);
                return true;
            }
            // Already confirmed to not lead to end
            if (notInPath.Contains(node))
                return false;
            // Recurse, tracking if there is a path
            recStack.Push(node);
            bool found = false;
            foreach (T neighbor in mapping[node])
                if (Search(neighbor))
                    found = true;
            recStack.Pop();
            // Add to set of nodes that don't lead to end
            if (!found)
                notInPath.Add(node);
            return found;
        }

        foreach (T start in startNodes)
            Search(start);
        paths = addedPaths;
        return pathFound;
    }

    /// <summary>
    /// Gets signal mapping of module--maps each signal to all its immediate inputs
    /// </summary>
    /// <param name="module"></param>
    /// <param name="connectionsThroughInstantiation">Input-to-output connections that go through an instance and should be further analyzed</param>
    /// <returns></returns>
    private static Dictionary<IModuleSpecificSignal, HashSet<IModuleSpecificSignal>> GetSignalMapping(IModule module, out Dictionary<(IModuleSpecificSignal, IModuleSpecificSignal), IInstantiation> connectionsThroughInstantiation)
    {
        // Find all input-output connections--dictionary maps signal to its immediate inputs
        // Assumes that all inputs to an instance are inputs to all of its outputs
        Dictionary<IModuleSpecificSignal, HashSet<IModuleSpecificSignal>> mapping = [];
        // Following will be used if we check paths through instantiations
        Dictionary<(IModuleSpecificSignal, IModuleSpecificSignal), IInstantiation> connections = [];

        void AddSignals(IModuleSpecificSignal outputSignal, IEnumerable<IModuleSpecificSignal> inputSignals, IInstantiation? instance = null)
        {
            if (mapping.TryGetValue(outputSignal, out HashSet<IModuleSpecificSignal>? signals))
                foreach (var signal in inputSignals)
                    signals.Add(signal);
            else
                mapping[outputSignal] = [.. inputSignals];

            if (instance is not null)
                foreach (var signal in inputSignals)
                    connections[(signal, outputSignal)] = instance;
        }

        // Signal behaviors--if it is an IAllowCircularSignals, just choose the disallowed ones
        foreach ((IModuleSpecificSignal signal, IBehavior behavior) in module.SignalBehaviors)
            AddSignals(signal, (behavior as IAllowCircularSignals)?.DisallowedCircularSignals ?? behavior.InputModuleSignals);

        // Instantiations--add all input signals for all output signals
        // Maybe explore later to see if there really is a connection
        foreach (IInstantiation instantiation in module.Instantiations.Where(i => i is not ICompiledObject))
        {
            List<INamedSignal> outputSignals = [];
            List<INamedSignal> inputSignals = [];
            foreach ((IPort port, INamedSignal signal) in instantiation.PortMapping)
                if (port.Direction == PortDirection.Input)
                    inputSignals.Add(signal);
                else
                    outputSignals.Add(signal);
            foreach (INamedSignal outputSignal in outputSignals)
                AddSignals(outputSignal, inputSignals);
        }

        // Derived signals--if derived signals can be recursive in future, check for IAllowRecursive
        foreach (IDerivedSignal derivedSignal in module.AllDerivedSignals)
        {
            // Linked signals
            if (derivedSignal.LinkedSignal is INamedSignal namedSignal and not ICompiledObject)
                AddSignals(namedSignal, [derivedSignal]);
            // Input signals to derived signal
            AddSignals(derivedSignal, derivedSignal.InputModuleSignals);
        }

        connectionsThroughInstantiation = connections;
        return mapping;
    }

    private static IEnumerable<(T, T)> GetPairsInPath<T>(List<T> path)
    {
        T prev = path.First();
        foreach (T node in path.Skip(1))
        {
            yield return (prev, node);
            prev = node;
        }
    }
}