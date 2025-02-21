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
        Assert.AreEqual("m1", m1.Name);

        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = new("s2", m1);
        Signal s3 = new("s3", m1);
        Signal s4 = new("s4", m1);

        INamedSignal[] namedSignals = [.. m1.NamedSignals];
        Assert.IsFalse(namedSignals.Length != 0);

        m1.AddNewPort(s1, PortDirection.Input);
        m1.AddNewPort(s2, PortDirection.Input);
        m1.AddNewPort(s4, PortDirection.Output);
        Assert.IsFalse(m1.Complete);

        namedSignals = [.. m1.NamedSignals];
        Assert.AreEqual(3, namedSignals.Length);
        Assert.IsTrue(namedSignals.Contains(s1));
        Assert.IsTrue(namedSignals.Contains(s2));
        Assert.IsTrue(namedSignals.Contains(s4));

        s3.AssignBehavior(s1.Not());
        m1.SignalBehaviors[s4] = new LogicBehavior(s3.And(s2));
        Assert.IsTrue(m1.Complete);

        namedSignals = [.. m1.NamedSignals];
        Assert.AreEqual(4, namedSignals.Length);
        Assert.IsTrue(namedSignals.Contains(s3));

        // Check VHDL
        string vhdl = m1.GetVhdl();
        string expectedVhdl = 
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity m1 is
            port (
                s1	: in	std_logic;
                s2	: in	std_logic;
                s4	: out	std_logic
            );
        end m1;

        architecture rtl of m1 is
            signal s3	: std_logic
        begin
            s3 <= (not (s1));
            s4 <= (s3 and s2);
        end rtl;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check SPICE
        string spice = m1.GetSpice();
        string expectedSpice = 
        """
        V_VDD VDD 0 5
        .MODEL NmosMod NMOS
        .MODEL PmosMod PMOS

        Rn0_0x0_res s1 n0_0x0_baseout 1m
        Mn0x0_p s3 n0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0x0_n s3 n0_0x0_baseout 0 0 NmosMod W=100u L=1u


        Rn1_0x0_res s3 n1_0x0_baseout 1m
        Rn1_1x0_res s2 n1_1x0_baseout 1m
        Mn1x0_pnand0 n1x0_nandout n1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn1x0_nnand0 n1x0_nandout n1_0x0_baseout n1x0_nand1 n1x0_nand1 NmosMod W=100u L=1u
        Mn1x0_pnand1 n1x0_nandout n1_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn1x0_nnand1 n1x0_nand1 n1_1x0_baseout 0 0 NmosMod W=100u L=1u
        Mn1x0_pnot s4 n1x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn1x0_nnot s4 n1x0_nandout 0 0 NmosMod W=100u L=1u


        Rn2x0_floating s4 0 1e9
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check SPICE as subcircuit
        string spiceSubcircuit = m1.GetSpice(true);
        string expectedSpiceSubcircuit = ".subckt m1 s1 s2 s4\n" + expectedSpice + "\n.ends m1";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpiceSubcircuit, spiceSubcircuit));
    }

    [TestMethod]
    public void InvalidBehaviorTest()
    {
        Module m1 = new("m1");
        Module m2 = new("m2");

        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = new("s2", m1);
        Signal s3 = new("s3", m2);
        Signal s4 = new("s4", m1);

        m1.AddNewPort(s1, PortDirection.Input);
        m1.AddNewPort(s4, PortDirection.Output);

        Assert.ThrowsException<Exception>(() => s1.AssignBehavior(0)); // Input port
        s2.AssignBehavior(0);
        ValueBehavior s4Behavior = new(1);
        s4.AssignBehavior(s4Behavior);
        Assert.ThrowsException<Exception>(() => s4.AssignBehavior(3)); // Too high dimension
        Assert.ThrowsException<Exception>(() => s4.AssignBehavior(s3.Not())); // Signal from another dimension as input

        // Check that s4 is still correct and s1 has no behavior
        Assert.AreEqual(s4Behavior, s4.Behavior);
        Assert.AreEqual(s4Behavior, m1.SignalBehaviors[s4]);
        Assert.IsNull(s1.Behavior);
        Assert.AreEqual(2, m1.SignalBehaviors.Count);
    }

    [TestMethod]
    public void ModuleUpdatedCallbackTest()
    {
        Module m1 = new("m1");
        Signal s1 = m1.GenerateSignal("s1");
        bool test = false;
        m1.ModuleUpdated += (sender, e) => test = true;
        Assert.IsFalse(test);
        s1.AssignBehavior(true);
        Assert.IsTrue(test);
    }

    [TestMethod]
    public void InstantiationTest()
    {
        Module instantiated = new("inst");
        Port p1 = instantiated.AddNewPort("p1", PortDirection.Input);
        Port p2 = instantiated.AddNewPort("p2", PortDirection.Output);
    }
}