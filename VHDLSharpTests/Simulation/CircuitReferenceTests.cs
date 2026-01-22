using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class CircuitReferenceTests
{
    [TestMethod]
    public void EqualityTest()
    {
        Module top = new("top");
        Signal top1 = top.GenerateSignal("top1");
        Signal top2 = top.GenerateSignal("top2");
        Module child = Util.GetSampleModule1();
        var i1 = top.AddNewInstantiation(child, "child1");
        i1.PortMapping.SetPort("s1", top1);
        i1.PortMapping.SetPort("s3", top2);

        SubmoduleReference subcktRef1 = new(top, [i1]);
        SubmoduleReference subcktRef2 = new(top, [i1]);
        Assert.AreEqual(subcktRef1, subcktRef2);

        SignalReference sigRef1 = subcktRef1.GetChildSignalReference("s1");
        SignalReference sigRef2 = subcktRef1.GetChildSignalReference("s1");
        Assert.AreEqual(sigRef1, sigRef2);
    }

    [TestMethod]
    public void AscensionTest()
    {
        Module top = new("top");
        Signal top1 = top.GenerateSignal("top1");
        Signal top2 = top.GenerateSignal("top2");
        Module child = Util.GetSampleModule1();
        var i1 = top.AddNewInstantiation(child, "child1");
        i1.PortMapping.SetPort("s1", top1);
        i1.PortMapping.SetPort("s3", top2);
        SubmoduleReference subcktRef1 = new(top, []);
        SubmoduleReference subcktRef2 = new(top, [i1]);

        SignalReference sigRef1 = subcktRef1.GetChildSignalReference("top1");
        SignalReference sigRef2 = subcktRef2.GetChildSignalReference("s1");
        SignalReference ascended2 = sigRef2.Ascend();
        Assert.AreEqual(sigRef1, ascended2);
    }

    [TestMethod]
    public void AscensionTestWithVector()
    {
        Module top = new("top");
        Vector top1 = top.GenerateVector("top1", 3);
        Signal top2 = top.GenerateSignal("top2");

        // Test with vector node fed into submodule
        Module child1 = Util.GetSampleModule1();
        var i1 = top.AddNewInstantiation(child1, "child1");
        i1.PortMapping.SetPort("s1", top1[1]);
        i1.PortMapping.SetPort("s3", top2);
        SubmoduleReference subcktRefTop = new(top, []);
        SubmoduleReference subcktRefChild1 = new(top, [i1]);

        SignalReference sigRef1 = subcktRefTop.GetChildSignalReference(top1[1]);
        SignalReference sigRef2 = subcktRefChild1.GetChildSignalReference("s1");
        SignalReference ascended2 = sigRef2.Ascend();
        Assert.AreEqual(sigRef1, ascended2);

        // Test with full vector fed into submodule
        Vector top3 = top.GenerateVector("top3", 2);
        Module child2 = Util.GetSampleModule2();
        var i2 = top.AddNewInstantiation(child2, "child2");
        i2.PortMapping.SetPort("s1", top1[1]);
        i2.PortMapping.SetPort("s3", top3);
        SubmoduleReference subcktRefChild2 = new(top, [i2]);

        SignalReference sigRef3 = subcktRefTop.GetChildSignalReference(top3);
        SignalReference sigRef4 = subcktRefChild2.GetChildSignalReference("s3");
        SignalReference ascended4 = sigRef4.Ascend();
        Assert.AreEqual(sigRef3, ascended4);

        SignalReference sigRef5 = subcktRefTop.GetChildSignalReference(top3[1]);
        ISingleNodeNamedSignal singleNodeRef = sigRef4.Signal.ToSingleNodeSignals.ElementAt(1);
        SignalReference sigRef6 = subcktRefChild2.GetChildSignalReference(singleNodeRef);
        SignalReference ascended6 = sigRef6.Ascend();
        Assert.AreEqual(sigRef5, ascended6);
    }
}