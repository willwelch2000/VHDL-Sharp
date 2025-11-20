using VHDLSharp.Algorithms;

namespace VHDLSharpTests;

[TestClass]
public class AlgorithmsTests
{
    [TestMethod]
    public void CircularityCheckTest()
    {
        static void PerformFirstPathCheck(Dictionary<int, List<int>> neighbors, bool expected, int[]? expectedPath = null)
        {
            if (expected)
            {
                Assert.IsTrue(ModuleAlgorithms.CheckForCircularity(neighbors, out var paths));
                Assert.IsTrue(ComparePaths(expectedPath!, [.. paths.First()]));
            }
            else
                Assert.IsFalse(ModuleAlgorithms.CheckForCircularity(neighbors, out _));
        }

        static void PerformCheck(Dictionary<int, List<int>> neighbors, bool expected, List<int[]> expectedPaths)
        {
            if (expected)
            {
                Assert.IsTrue(ModuleAlgorithms.CheckForCircularity(neighbors, out var paths, true));
                foreach (List<int> path in paths)
                {
                    bool found = false;
                    foreach (int[] expectedPath in expectedPaths)
                    {
                        if (ComparePaths(expectedPath, [.. path]))
                        {
                            found = true;
                            break;
                        }
                    }
                    Assert.IsTrue(found);
                }
            }
            else
                Assert.IsFalse(ModuleAlgorithms.CheckForCircularity(neighbors, out _));
        }

        static bool ComparePaths(int[] path1, int[] path2)
        {
            int length = path1.Length;
            if (!path2.Contains(path1[0]) || length != path2.Length)
                return false;
            int index = Array.IndexOf(path2, path1[0]);
            foreach (int node in path1)
            {
                if (node != path2[index])
                    return false;
                index = (index + 1) % length;
            }
            return true;
        }

        Dictionary<int, List<int>> neighbors = new()
        {
            {1, [2]},
            {2, [3]},
            {3, [4, 6]},
            {4, [1]},
            {5, [2]},
            {6, []},
        };
        PerformFirstPathCheck(neighbors, true, [2, 3, 4, 1]);
        neighbors = new()
        {
            {1, [2, 3, 5]},
            {2, [4]},
            {3, [4]},
            {4, []},
            {5, [6]},
            {6, [4]},
        };
        PerformFirstPathCheck(neighbors, false);
        neighbors = new()
        {
            {2, []},
            {3, [7]},
            {4, [3]},
            {6, [3]},
            {7, [9]},
            {9, [2, 6]},
        };
        PerformFirstPathCheck(neighbors, true, [7, 9, 6, 3]);
        neighbors = new()
        {
            {1, [2]},
            {2, [3]},
            {3, [4]},
            {4, [5, 6]},
            {5, [2]},
            {6, [7]},
            {7, [3]},
        };
        PerformCheck(neighbors, true, [[2, 3, 4, 5], [7, 3, 4, 6]]);
        neighbors = new()
        {
            {1, [2]},
            {2, [1]},
        };
        PerformCheck(neighbors, true, [[1, 2]]);
        neighbors = new()
        {
            {1, [1]},
        };
        PerformFirstPathCheck(neighbors, true, [1]);
    }
}