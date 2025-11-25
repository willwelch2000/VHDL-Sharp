using VHDLSharp.Algorithms;
using VHDLSharp.Behaviors;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class AlgorithmsTests
{
    [TestMethod]
    public void CircularityCheckTest()
    {
        static void PerformFirstPathCheck(Dictionary<int, IEnumerable<int>> neighbors, bool expected, int[]? expectedPath = null)
        {
            if (expected)
            {
                Assert.IsTrue(ModuleAlgorithms.CheckForCircularity(neighbors, out var paths));
                Assert.IsTrue(ComparePaths(expectedPath!, [.. paths.First()]));
            }
            else
                Assert.IsFalse(ModuleAlgorithms.CheckForCircularity(neighbors, out _));
        }

        static void PerformCheck(Dictionary<int, IEnumerable<int>> neighbors, bool expected, List<int[]> expectedPaths)
        {
            if (expected)
            {
                Assert.IsTrue(ModuleAlgorithms.CheckForCircularity(neighbors, out var paths, true));
                Assert.AreEqual(expectedPaths!.Count, paths.Count);
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

        Dictionary<int, IEnumerable<int>> neighbors = new()
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

    [TestMethod]
    public void CheckConnectionsTest()
    {
        static void PerformCheck(Dictionary<int, IEnumerable<int>> neighbors, bool expected, List<int> start, HashSet<int> end, List<int[]> expectedPaths)
        {
            if (expected)
            {
                Assert.IsTrue(ModuleAlgorithms.CheckForConnections(neighbors, start, end, out var paths));
                Assert.AreEqual(expectedPaths!.Count, paths.Count);
                foreach (List<int> path in paths)
                {
                    bool found = false;
                    foreach (int[] expectedPath in expectedPaths)
                    {
                        if (expectedPath.SequenceEqual(path))
                        {
                            found = true;
                            break;
                        }
                    }
                    Assert.IsTrue(found);
                }
            }
            else
                Assert.IsFalse(ModuleAlgorithms.CheckForConnections(neighbors, start, end, out _));
        }

        Dictionary<int, IEnumerable<int>> neighbors = new()
        {
            {1, [2, 5]},
            {2, [3, 4]},
            {3, [6]},
            {4, []},
            {5, []},
            {6, []},
            {7, [1]}
        };
        PerformCheck(neighbors, true, [1, 7], [4, 6], [[1, 2, 3, 6], [1, 2, 4], [7, 1, 2, 3, 6], [7, 1, 2, 4]]);
        PerformCheck(neighbors, true, [7, 1], [4, 6], [[1, 2, 3, 6], [1, 2, 4], [7, 1, 2, 3, 6], [7, 1, 2, 4]]);

        neighbors = new()
        {
            {1, [2]},
            {2, [3, 4]},
            {3, []},
            {4, []}
        };
        PerformCheck(neighbors, false, [4], [1], []);
    }

    [TestMethod]
    public void CircularSignalCheckTest()
    {
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Signal s2 = module.GenerateSignal("s2");
        Signal s3 = module.GenerateSignal("s3");
        s2.AssignBehavior(s1);
        Assert.IsFalse(ModuleAlgorithms.CheckForCircularSignals(module, out List<List<IModuleSpecificSignal>> paths));
        s1.AssignBehavior(s2);
        Assert.IsTrue(ModuleAlgorithms.CheckForCircularSignals(module, out paths));
        s1.RemoveBehavior();
        Assert.IsFalse(ModuleAlgorithms.CheckForCircularSignals(module, out paths));

        // Test legal dynamic behavior input
        DynamicBehavior dynamic = s1.AssignBehavior(new DynamicBehavior());
        dynamic.Add(s3.RisingEdge(), s2.ToBehavior());
        Assert.IsFalse(ModuleAlgorithms.CheckForCircularSignals(module, out paths));
        Assert.IsTrue(module.ValidityManager.IsValid(out Exception? exception));

        // Test illegal recursion in dynamic behavior
        dynamic.Add(s3.IsHigh(), s2.ToBehavior());
        Assert.IsTrue(ModuleAlgorithms.CheckForCircularSignals(module, out paths));
        Assert.IsFalse(module.ValidityManager.IsValid(out exception));
        Assert.IsInstanceOfType<CircularSignalException>(exception);
    }

    [TestMethod]
    public void CheckPortConnectionTest()
    {
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Signal s2 = module.GenerateSignal("s2");
        Signal s3 = module.GenerateSignal("s3");
        Signal s4 = module.GenerateSignal("s4");
        Signal s5 = module.GenerateSignal("s5");
        Signal s6 = module.GenerateSignal("s6");

        Port p1 = module.AddNewPort(s1, PortDirection.Input);
        Port p2 = module.AddNewPort(s2, PortDirection.Input);
        Port p3 = module.AddNewPort(s3, PortDirection.Output);
        Port p4 = module.AddNewPort(s4, PortDirection.Output);
        Port p6 = module.AddNewPort(s6, PortDirection.Output);

        s3.AssignBehavior(s1);
        s5.AssignBehavior(s3);
        s4.AssignBehavior(s5);
        s6.AssignBehavior(1);

        Dictionary<IPort, Dictionary<IPort, bool>> cache = [];
        Assert.IsTrue(ModuleAlgorithms.CheckPortConnection(p1, p3, cache));
        Assert.IsTrue(ModuleAlgorithms.CheckPortConnection(p1, p4, cache));
        Assert.IsFalse(ModuleAlgorithms.CheckPortConnection(p1, p6, cache));
        Assert.IsFalse(ModuleAlgorithms.CheckPortConnection(p2, p3, cache));
        Assert.IsFalse(ModuleAlgorithms.CheckPortConnection(p2, p4, cache));
        Assert.IsFalse(ModuleAlgorithms.CheckPortConnection(p2, p6, cache));
    }
}