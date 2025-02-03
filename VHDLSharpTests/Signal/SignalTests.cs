using VHDLSharp.LogicTree;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class SignalTests
{
    [TestMethod]
    public void BasicTest()
    {
        Module module1 = new("m1");
        Signal s1 = module1.GenerateSignal("s1");

        Assert.AreEqual("s1", s1.Name);
        Assert.AreEqual(module1, s1.ParentModule);
        Assert.IsNull(s1.ParentSignal);
        Assert.AreEqual(s1, s1.TopLevelSignal);
        Assert.AreEqual("s1", s1.ToSpice());
        Assert.AreEqual("s1", s1.ToString());

        Assert.AreEqual(1, s1.Dimension.NonNullValue);
        Assert.AreEqual("std_logic", s1.VhdlType);
        Assert.AreEqual(0, s1.ChildSignals.Count());
        SingleNodeNamedSignal[] singleNodeSignals = [.. s1.ToSingleNodeSignals];
        Assert.AreEqual(1, singleNodeSignals.Length);
        Assert.AreEqual(s1, singleNodeSignals[0]);
        Assert.AreEqual("s1", s1.ToLogicString());
        Assert.AreEqual("signal s1\t: std_logic", s1.ToVhdl());

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => s1[1]);
        Assert.AreEqual(s1, s1[0]);
    }

    [TestMethod]
    public void CanCombineTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        Signal s1 = new("s1", module1);
        Signal s2 = new("s2", module1);
        Signal s3 = new("s3", module2);
        Literal literal1 = new(6, 4);
        Literal literal2 = new(1, 1);
        Vector v1 = new("v1", module1, 4);

        Assert.IsTrue(s1.CanCombine(s2));
        Assert.IsTrue(s1.CanCombine(literal2));
        Assert.IsFalse(s1.CanCombine(literal1));
        Assert.IsFalse(s1.CanCombine(v1));
        Assert.IsTrue(s1.CanCombine([s2, literal2]));
        // False because they come from different modules
        Assert.IsFalse(s1.CanCombine([s2, s3]));
    }

    [TestMethod]
    public void LogicTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        Signal s1 = new("s1", module1);
        Signal s2 = new("s2", module1);
        Signal s3 = new("s3", module2);
        Signal s4 = new("s3", module1);

        // Not
        Not<ISignal> not = s1.Not();
        Assert.AreEqual(s1, not.Input);

        // And
        Assert.ThrowsException<Exception>(() => s1.And(s3));
        And<ISignal> and = s1.And([s2, s4]);
        ILogicallyCombinable<ISignal>[] andInputs = [.. and.Inputs];
        Assert.AreEqual(3, andInputs.Length);
        Assert.IsTrue(andInputs.Contains(s1));
        Assert.IsTrue(andInputs.Contains(s2));
        Assert.IsTrue(andInputs.Contains(s4));

        // Or
        Assert.ThrowsException<Exception>(() => s1.Or(s3));
        Or<ISignal> or = s1.Or([s2, s4]);
        ILogicallyCombinable<ISignal>[] orInputs = [.. or.Inputs];
        Assert.AreEqual(3, orInputs.Length);
        Assert.IsTrue(orInputs.Contains(s1));
        Assert.IsTrue(orInputs.Contains(s2));
        Assert.IsTrue(orInputs.Contains(s4));
    }
}