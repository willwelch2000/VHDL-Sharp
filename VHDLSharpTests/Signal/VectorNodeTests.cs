using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class VectorNodeTests
{
    [TestMethod]
    public void BasicTest()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module1 = new("m1");
        Vector v1 = module1.GenerateVector("v1", 4);
        VectorNode node0 = v1.ToSingleNodeSignals.First();

        Assert.AreEqual(v1, node0.Vector);
        Assert.AreEqual(v1, node0.ParentSignal);
        Assert.AreEqual(1, node0.Dimension.NonNullValue);
        Assert.AreEqual(module1, node0.ParentModule);
        Assert.AreEqual(v1, node0.TopLevelSignal);
        ISignal[] singleNodeSignals = [.. node0.ToSingleNodeSignals];
        Assert.AreEqual(1, singleNodeSignals.Length);
        Assert.AreEqual(node0, singleNodeSignals[0]);

        Assert.AreEqual("v1[0]", node0.Name);
        Assert.AreEqual(0, node0.Node);
        Assert.AreEqual("v1_0", node0.GetSpiceName());
        Assert.AreEqual("v1[0]", node0.ToLogicString());
        Assert.AreEqual("signal v1_0\t: std_logic", node0.GetVhdlDeclaration());

        Module module2 = new("m2");
        Vector v2 = module2.GenerateVector("v2", 4);
        VectorNode v2Node = v2[2];
        PortMapping mapping = new(module2, module1);
        Assert.IsFalse(v2Node.IsPartOfPortMapping(mapping, out _));
        mapping[new Port(v2, PortDirection.Input)] = v1;
        Assert.IsTrue(v2Node.IsPartOfPortMapping(mapping, out INamedSignal? equivalentSignal));
        Assert.AreEqual(equivalentSignal, v1[2]);
    }

    [TestMethod]
    public void CanCombineTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        Vector v1 = module1.GenerateVector("v1", 4);
        VectorNode node0 = v1.ToSingleNodeSignals.First();

        Signal s1 = new("s1", module1);
        Vector v2 = new("v2", module1, 2);
        Signal s2 = new("s2", module2);

        Assert.IsTrue(node0.CanCombine(s1));
        Assert.IsFalse(node0.CanCombine(v2));
        Assert.IsFalse(node0.CanCombine(s2));
        Assert.IsFalse(node0.CanCombine([s1, s2]));
    }
}