using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Modules;

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
        // Find all input-output connections

        // Turn that into Dictionary of <output, list of input signals>
        return false;
    }

    /// <summary>
    /// Checks for circular assignment given dictionary mapping something to its one-directional neighbors
    /// https://www.geeksforgeeks.org/dsa/detect-cycle-in-a-graph/
    /// </summary>
    /// <param name="mapping"></param>
    /// <param name="paths">Paths of nodes that form a circle</param>
    /// <param name="findAllPaths">If true, find all circular paths and not just first</param>
    /// <returns></returns>
    public static bool CheckForCircularity<T>(Dictionary<T, List<T>> mapping, out List<List<T>> paths, bool findAllPaths = false) where T : notnull
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