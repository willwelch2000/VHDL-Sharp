using VHDLSharp.Behaviors;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class ValueBehaviorTests
{
    [TestMethod]
    public void SingleDimensionTest()
    {
        Module module1 = new("module1");
        Signal s1 = module1.GenerateSignal("s1");
        Vector v2 = module1.GenerateVector("v2", 3);
        ValueBehavior behavior = new(1);
        
        Assert.AreEqual(1, behavior.Value);
        Assert.AreEqual(new Dimension(1, null), behavior.Dimension);
        Assert.IsNull(behavior.ParentModule);
        Assert.IsFalse(behavior.NamedInputSignals.Any());

        // Compatibility
        Assert.IsTrue(behavior.IsCompatible(s1));
        Assert.IsTrue(behavior.IsCompatible(v2));

        // Check Spice
        string spice = behavior.ToSpice(s1, "0");
        string expectedSpice = "Vn0x0_value s1 0 5";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check VHDL
        string vhdl = behavior.ToVhdl(s1);
        string expectedVhdl = "s1 <= \"1\";";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));
    }

    [TestMethod]
    public void MultiDimensionTest()
    {
        Module module1 = new("module1");
        Signal s1 = module1.GenerateSignal("s1");
        Vector v2 = module1.GenerateVector("v2", 4);
        Vector v3 = module1.GenerateVector("v3", 3);
        ValueBehavior behavior1 = new(10);
        ValueBehavior behavior2 = new(6);

        // Basic stuff
        Assert.AreEqual(10, behavior1.Value);
        Assert.AreEqual(6, behavior2.Value);
        Assert.AreEqual(new Dimension(4, null), behavior1.Dimension);
        Assert.AreEqual(new Dimension(3, null), behavior2.Dimension);
        Assert.IsNull(behavior1.ParentModule);
        Assert.IsNull(behavior2.ParentModule);
        Assert.IsFalse(behavior1.NamedInputSignals.Any());
        Assert.IsFalse(behavior2.NamedInputSignals.Any());

        // Compatibility
        Assert.IsFalse(behavior1.IsCompatible(s1));
        Assert.IsFalse(behavior2.IsCompatible(s1));
        Assert.IsTrue(behavior1.IsCompatible(v2));
        Assert.IsTrue(behavior2.IsCompatible(v2));
        Assert.IsFalse(behavior1.IsCompatible(v3));
        Assert.IsTrue(behavior2.IsCompatible(v3));

        // Check Spice
        string spice = behavior1.ToSpice(v2, "0");
        string expectedSpice = 
        """
        Vn0x0_value v2_0 0 0
        Vn0x1_value v2_1 0 5
        Vn0x2_value v2_2 0 0
        Vn0x3_value v2_3 0 5
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check VHDL
        string vhdl = behavior1.ToVhdl(v2);
        string expectedVhdl = "v2 <= \"1010\";";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));
    }
}