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
    /// <param name="path">Path of nodes that form a circle</param>
    /// <returns></returns>
    public static bool CheckForCircularity<T>(Dictionary<T, List<T>> mapping, [MaybeNullWhen(false)] out List<T> path) where T : notnull
    {
        HashSet<T> visited = [];
        Stack<T> recStack = [];
        path = null;

        bool Search(T node)
        {
            if (recStack.Contains(node))
            {
                recStack.Push(node);
                return true;
            }
            if (visited.Contains(node))
                return false;
            visited.Add(node);
            recStack.Push(node);
            foreach (T neighbor in mapping[node])
            {
                if (Search(neighbor))
                    return true;
            }
            recStack.Pop();
            return false;
        }
        
        foreach (T node in mapping.Keys)
            if (Search(node))
            {
                // Last thing added is node that completes the path, so it is the first and last node
                T first = recStack.Pop();
                path = [first];
                bool skip = true;
                foreach (T stackNode in recStack.Reverse())
                {
                    if (!skip)
                        path.Add(stackNode);
                    if (skip && stackNode.Equals(first))
                        skip = false;
                }
                return true;
            }
        return false;
    }
}