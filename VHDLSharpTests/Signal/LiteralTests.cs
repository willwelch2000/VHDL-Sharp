using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class LiteralTests
{
    [TestMethod]
    public void BasicTest()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Literal(-1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Literal(1, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Literal(8, 3));

        Literal literal = new(5, 4);

        Assert.AreEqual(5, literal.Value);
        Assert.AreEqual(4, literal.Dimension.NonNullValue);
        Assert.IsNull(literal.ParentModule);
        Assert.IsNull(literal.ParentSignal);
        Assert.AreEqual(literal, literal.TopLevelSignal);
    }

    [TestMethod]
    public void CanCombineTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        Literal literal1 = new(5, 4);
        Literal literal2 = new(6, 4);
        Literal literal3 = new(6, 5);
        Vector v1 = new("v1", module1, 4);
        Signal s1 = new("s1", module1);
        Vector v2 = new("v2", module2, 4);

        Assert.IsTrue(literal1.CanCombine(literal2));
        Assert.IsFalse(literal1.CanCombine(literal3));
        Assert.IsTrue(literal1.CanCombine(v1));
        Assert.IsFalse(literal1.CanCombine(s1));
        Assert.IsTrue(literal1.CanCombine([literal2, v1]));
        // False because they come from different modules
        Assert.IsFalse(literal1.CanCombine([v1, v2]));
    }

    [TestMethod]
    public void ToLogicStringTest()
    {
        Literal literal1 = new(0, 1);
        Literal literal2 = new(6, 4);
        Literal literal3 = new(6, 5);

        Assert.AreEqual("\"0\"", literal1.ToLogicString());
        Assert.AreEqual("\"0110\"", literal2.ToLogicString());
        Assert.AreEqual("\"00110\"", literal3.ToLogicString());
    }
}