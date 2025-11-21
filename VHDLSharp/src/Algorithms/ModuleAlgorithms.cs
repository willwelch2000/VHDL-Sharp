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
    /// <returns></returns>
    public static bool CheckForCircularSignals(IModule module)
    {
        // Find all input-output connections--dictionary maps signal to its immediate inputs
        // Assume that all inputs to an instance are inputs to all of its outputs
        Dictionary<IModuleSpecificSignal, HashSet<IModuleSpecificSignal>> mapping = [];
        // Following will be used if we check paths through instantiations
        // HashSet<(IModuleSpecificSignal, IModuleSpecificSignal)> connectionsWithoutInstantiation = [];
        // Dictionary<(IModuleSpecificSignal, IModuleSpecificSignal), IInstantiation> connectionsThroughInstantiation = [];

        void AddSignals(IModuleSpecificSignal outputSignal, IEnumerable<IModuleSpecificSignal> inputSignals)
        {
            if (mapping.TryGetValue(outputSignal, out HashSet<IModuleSpecificSignal>? signals))
                foreach (var signal in inputSignals)
                    signals.Add(signal);
            else
                mapping[outputSignal] = [.. inputSignals];
        }

        // Signal behaviors
        foreach ((IModuleSpecificSignal signal, IBehavior behavior) in module.SignalBehaviors)
            AddSignals(signal, behavior.InputModuleSignals);

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

        // Derived signals
        foreach (IDerivedSignal derivedSignal in module.AllDerivedSignals)
        {
            // Linked signals
            if (derivedSignal.LinkedSignal is INamedSignal namedSignal and not ICompiledObject)
                AddSignals(namedSignal, [derivedSignal]);
            // Input signals to derived signal
            AddSignals(derivedSignal, derivedSignal.InputModuleSignals);
        }

        // Check if there are circular paths
        return CheckForCircularity(mapping, out List<List<IModuleSpecificSignal>> paths);

        // In future version, check if there are paths that don't go through instances
        // If all paths go through an instance, look in the instance to see if it really does go in a path through it
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
            foreach (T neighbor in mapping[node])
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
                return;
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
}