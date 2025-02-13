using VHDLSharp.Behaviors;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class VectorTests()
{
    [TestMethod]
    public void BasicTest()
    {
        Module module1 = new("m1");
        Vector v1 = module1.GenerateVector("v1", 4);

        Assert.AreEqual("v1", v1.Name);
        Assert.AreEqual(module1, v1.ParentModule);
        Assert.IsNull(v1.ParentSignal);
        Assert.AreEqual(v1, v1.TopLevelSignal);
        Assert.AreEqual("v1", v1.ToString());

        Assert.AreEqual(4, v1.Dimension.NonNullValue);
        Assert.AreEqual("std_logic_vector(3 downto 0)", v1.VhdlType);
        VectorNode[] vectorNodes = [.. v1.ChildSignals];
        Assert.AreEqual(4, vectorNodes.Length);
        Assert.AreEqual("v1", v1.ToLogicString());
        Assert.AreEqual("signal v1\t: std_logic_vector(3 downto 0)", v1.GetVhdlDeclaration());
    }

    [TestMethod]
    public void CanCombineTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        Vector v1 = new("v1", module1, 4);
        Vector v2 = new("v2", module1, 4);
        Vector v3 = new("v3", module2, 4);
        Vector v4 = new("v4", module1, 2);
        Signal s1 = new("s1", module1);
        Literal literal1 = new(6, 4);
        Literal literal2 = new(1, 1);

        Assert.IsTrue(v1.CanCombine(v2));
        Assert.IsTrue(v1.CanCombine(literal1));
        Assert.IsFalse(v1.CanCombine(v3));
        Assert.IsFalse(v1.CanCombine(v4));
        Assert.IsFalse(v1.CanCombine(literal2));
        Assert.IsFalse(v1.CanCombine(s1));
        Assert.IsTrue(v1.CanCombine([v2, literal1]));
        // False because they come from different modules
        Assert.IsFalse(v1.CanCombine([literal1, v3]));
    }

    [TestMethod]
    public void LogicTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        Vector v1 = new("v1", module1, 4);
        Vector v2 = new("v2", module1, 4);
        Vector v3 = new("v3", module2, 4);
        Vector v4 = new("v4", module1, 4);
        Vector v5 = new("v5", module1, 2);

        // Not
        Not<ISignal> not = v1.Not();
        Assert.AreEqual(v1, not.Input);

        // And
        Assert.ThrowsException<Exception>(() => v1.And(v3));
        Assert.ThrowsException<Exception>(() => v1.And(v5));
        And<ISignal> and = v1.And([v2, v4]);
        ILogicallyCombinable<ISignal>[] andInputs = [.. and.Inputs];
        Assert.AreEqual(3, andInputs.Length);
        Assert.IsTrue(andInputs.Contains(v1));
        Assert.IsTrue(andInputs.Contains(v2));
        Assert.IsTrue(andInputs.Contains(v4));

        // Or
        Assert.ThrowsException<Exception>(() => v1.Or(v3));
        Assert.ThrowsException<Exception>(() => v1.Or(v5));
        Or<ISignal> or = v1.Or([v2, v4]);
        ILogicallyCombinable<ISignal>[] orInputs = [.. or.Inputs];
        Assert.AreEqual(3, orInputs.Length);
        Assert.IsTrue(orInputs.Contains(v1));
        Assert.IsTrue(orInputs.Contains(v2));
        Assert.IsTrue(orInputs.Contains(v4));
    }

    [TestMethod]
    public void AssignBehaviorTest()
    {
        Module module1 = new("m1");
        Vector v1 = module1.GenerateVector("v1", 4);
        Vector v2 = module1.GenerateVector("v2", 4);

        Assert.IsNull(v1.Behavior);
        v1.AssignBehavior(3);
        DigitalBehavior behavior = v1.Behavior!;
        Assert.IsTrue(behavior is ValueBehavior valueBehavior && valueBehavior.Value == 3);

        v1.Behavior = null;
        Assert.IsNull(v1.Behavior);

        v1.AssignBehavior(v2);
        behavior = v1.Behavior!;
        Assert.IsTrue(behavior is LogicBehavior logicBehavior && logicBehavior.LogicExpression.InnerExpression == v2);

        Literal literal = new(12, 4);
        DigitalBehavior literalBehavior = new LogicBehavior(literal);
        v1.AssignBehavior(literalBehavior);
        behavior = v1.Behavior!;
        Assert.IsTrue(behavior is LogicBehavior logicBehavior2 && logicBehavior2.LogicExpression.InnerExpression == literal);
    }
}