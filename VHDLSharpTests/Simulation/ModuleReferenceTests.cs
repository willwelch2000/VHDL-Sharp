using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class ModuleReferenceTests
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
}