using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class ModuleTests
{
    [TestMethod]
    public void BasicTest()
    {
        Module m1 = new("m1");
        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = new("s2", m1);
        Signal s3 = new("s3", m1);
        Signal s4 = new("s4", m1);

        INamedSignal[] namedSignals = [.. m1.NamedSignals];
        Assert.IsFalse(namedSignals.Length != 0);

        m1.AddNewPort(s1, PortDirection.Input);
        m1.AddNewPort(s2, PortDirection.Input);
        m1.AddNewPort(s4, PortDirection.Output);

        namedSignals = [.. m1.NamedSignals];
        Assert.AreEqual(3, namedSignals.Length);
        Assert.IsTrue(namedSignals.Contains(s1));
        Assert.IsTrue(namedSignals.Contains(s2));
        Assert.IsTrue(namedSignals.Contains(s4));

        Assert.ThrowsException<Exception>(() => s1.AssignBehavior(0));
        s3.AssignBehavior(s1.Not());
        m1.SignalBehaviors[s4] = new LogicBehavior(s3.And(s2));
    }
}