
using VHDLSharp.Modules;
using VHDLSharp.Signals;

[TestClass]
public class LiteralNodeTests
{
    [TestMethod]
    public void ValueTest()
    {
        Literal literal = new(5, 4);

        // Children
        LiteralNode[] bits = [.. literal.ToSingleNodeSignals];
        Assert.AreEqual(4, bits.Length);
        Assert.AreEqual(true, bits[0].Value);
        Assert.AreEqual(false, bits[1].Value);
        Assert.AreEqual(true, bits[2].Value);
        Assert.AreEqual(false, bits[3].Value);
        for (int i = 0; i < bits.Length; i++)
            Assert.AreEqual(bits[i], literal[i]);
    }

    [TestMethod]
    public void BasicTest()
    {
        Literal literal = new(5, 4);
        LiteralNode child0 = literal.ToSingleNodeSignals.First();

        Assert.AreEqual(literal, child0.Literal);
        Assert.AreEqual(1, child0.Dimension.NonNullValue);
        Assert.IsNull(child0.ParentModule);
        ISignal[] singleNodeSignals = [.. child0.ToSingleNodeSignals];
        Assert.AreEqual(1, singleNodeSignals.Length);
        Assert.AreEqual(child0, singleNodeSignals[0]);
        Assert.AreEqual(literal, child0.ParentSignal);
        Assert.AreEqual(literal, child0.TopLevelSignal);
        Assert.AreEqual(0, child0.ChildSignals.Count());
    }

    [TestMethod]
    public void CanCombineTest()
    {
        Literal literal = new(5, 4);
        LiteralNode child0 = literal.ToSingleNodeSignals.First();

        Module module1 = new("m1");
        Module module2 = new("m2");
        Signal s1 = new("s1", module1);
        Vector v1 = new("v1", module1, 2);
        Signal s2 = new("s2", module2);

        Assert.IsTrue(child0.CanCombine(s1));
        Assert.IsFalse(child0.CanCombine(v1));
        Assert.IsTrue(child0.CanCombine(s2));
        Assert.IsFalse(child0.CanCombine([s1, s2]));
        Assert.AreEqual("True", child0.ToLogicString());
        Assert.AreEqual("VDD", child0.ToSpice());
    }
}