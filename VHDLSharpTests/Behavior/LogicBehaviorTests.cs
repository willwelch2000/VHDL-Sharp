
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Behaviors;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class LogicBehaviorTests
{
    private Signal? s1;
    private Signal? s2;
    private Signal? s3;

    private Module Module1 { get; } = new("m1");
    private Signal S1
    {
        get
        {
            s1 ??= new("s1", Module1);
            return s1;
        }
    }
    private Signal S2
    {
        get
        {
            s2 ??= new("s2", Module1);
            return s2;
        }
    }
    private Signal S3
    {
        get
        {
            s3 ??= new("s3", Module1);
            return s3;
        }
    }

    [TestMethod]
    public void AndExpressionTest()
    {
        Module module1 = Module1;
        Signal s1 = S1;
        Signal s2 = S2;
        Signal s3 = S3;

        LogicBehavior behavior = new(s1.And(s2));

        // Check Spice
        string spice = behavior.ToSpice(s3, "0");
        string expectedSpice = 
        """
        Rn0_0x0_res s1 n0_0x0_baseout 1m
        Rn0_1x0_res s2 n0_1x0_baseout 1m
        Mn0x0_pnand0 n0x0_nandout n0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0x0_nnand0 n0x0_nandout n0_0x0_baseout n0x0_nand1 n0x0_nand1 NmosMod W=100u L=1u
        Mn0x0_pnand1 n0x0_nandout n0_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0x0_nnand1 n0x0_nand1 n0_1x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0x0_pnot s3 n0x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0x0_nnot s3 n0x0_nandout 0 0 NmosMod W=100u L=1u
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check VHDL
        string vhdl = behavior.ToVhdl(s3);
        string expectedVhdl = "s3 <= (s1 and s2);";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));

        // Check Spice# entities
        IEntity[] entities = [.. behavior.GetSpiceSharpEntities(s3, "0")];
        Assert.AreEqual(entities.Length, 9);
        Resistor resistor0 = entities.First(e => e.Name == "Rn0_0x0_res") as Resistor ?? throw new();
        Assert.IsTrue(resistor0.Nodes.SequenceEqual(["s1", "n0_0x0_baseout"]));
        Resistor resistor1 = entities.First(e => e.Name == "Rn0_1x0_res") as Resistor ?? throw new();
        Assert.IsTrue(resistor1.Nodes.SequenceEqual(["s2", "n0_1x0_baseout"]));
        Mosfet1 pnand0 = entities.First(e => e.Name == "Mn0x0_pnand0") as Mosfet1 ?? throw new();
        Assert.IsTrue(pnand0.Nodes.SequenceEqual(["n0x0_nandout", "n0_0x0_baseout", "VDD", "VDD"]));
        Assert.AreEqual("PmosMod", pnand0.Model);
        Mosfet1 nnand0 = entities.First(e => e.Name == "Mn0x0_nnand0") as Mosfet1 ?? throw new();
        Assert.IsTrue(nnand0.Nodes.SequenceEqual(["n0x0_nandout", "n0_0x0_baseout", "n0x0_nand1", "n0x0_nand1"]));
        Assert.AreEqual("NmosMod", nnand0.Model);
        Mosfet1 pnand1 = entities.First(e => e.Name == "Mn0x0_pnand1") as Mosfet1 ?? throw new();
        Assert.IsTrue(pnand1.Nodes.SequenceEqual(["n0x0_nandout", "n0_1x0_baseout", "VDD", "VDD"]));
        Assert.AreEqual("PmosMod", pnand1.Model);
        Mosfet1 nnand1 = entities.First(e => e.Name == "Mn0x0_nnand1") as Mosfet1 ?? throw new();
        Assert.IsTrue(nnand1.Nodes.SequenceEqual(["n0x0_nand1", "n0_1x0_baseout", "0", "0"]));
        Assert.AreEqual("NmosMod", nnand1.Model);
        Mosfet1 pnot = entities.First(e => e.Name == "Mn0x0_pnot") as Mosfet1 ?? throw new();
        Assert.IsTrue(pnot.Nodes.SequenceEqual(["n0x0_andout", "n0x0_nandout", "VDD", "VDD"]));
        Assert.AreEqual("PmosMod", pnot.Model);
        Mosfet1 nnot = entities.First(e => e.Name == "Mn0x0_nnot") as Mosfet1 ?? throw new();
        Assert.IsTrue(nnot.Nodes.SequenceEqual(["n0x0_andout", "n0x0_nandout", "0", "0"]));
        Assert.AreEqual("NmosMod", nnot.Model);
        Resistor resistorOut = entities.First(e => e.Name == "Rn0x0_connect") as Resistor ?? throw new();
        Assert.IsTrue(resistorOut.Nodes.SequenceEqual(["n0x0_andout", "s3"]));

        // Check named signals
        NamedSignal[] behaviorSignals = [.. behavior.NamedInputSignals];
        Assert.AreEqual(2, behaviorSignals.Length);
        Assert.AreEqual(s1, behaviorSignals[0]);
        Assert.AreEqual(s2, behaviorSignals[1]);

        // Check simple stuff
        Assert.AreEqual(1, behavior.Dimension.Value);
        Assert.AreEqual(module1, behavior.ParentModule);
    }

    [TestMethod]
    public void ExceptionTests()
    {
        Module module1 = Module1;
        Signal s1 = S1;
        Signal s2 = S2;
        Signal s3 = S3;
        LogicBehavior behavior = new(s1.And(s2));

        Vector v4 = new("v4", module1, 2);
        Assert.IsTrue(behavior.IsCompatible(s3));
        Assert.IsFalse(behavior.IsCompatible(v4));
        Assert.ThrowsException<IncompatibleSignalException>(() => behavior.ToSpice(v4, "0"));
        Assert.ThrowsException<IncompatibleSignalException>(() => behavior.ToVhdl(v4));
        Assert.ThrowsException<IncompatibleSignalException>(() => behavior.GetSpiceSharpEntities(v4, "0"));
    }

    [TestMethod]
    public void MultiDimensionAndTest()
    {
        Module module1 = Module1;
        Vector v1 = new("v1", module1, 3);
        Vector v2 = new("v2", module1, 3);
        Vector v3 = new("v3", module1, 3);
        Vector v4 = new("v4", module1, 4);
        LogicBehavior behavior = new(v1.And(v2));

        // Check compatibility
        Assert.IsTrue(behavior.IsCompatible(v3));
        Assert.IsFalse(behavior.IsCompatible(v4));
        Assert.IsFalse(behavior.IsCompatible(S1));

        // Check dimension
        Assert.AreEqual(3, behavior.Dimension.Value);

        // Check VHDL
        string vhdl = behavior.ToVhdl(v3);
        string expectedVhdl = "v3 <= (v1 and v2);";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));
    }

    // TODO expression with literal
}