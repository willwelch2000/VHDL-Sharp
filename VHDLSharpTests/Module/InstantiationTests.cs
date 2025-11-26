using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Behaviors;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharpTests;

[TestClass]
public class InstantiationTests
{
    [TestMethod]
    public void BasicTest()
    {
        Module parentMod = new("parentMod");
        Module instanceMod = new("instanceMod");
        Port p1 = instanceMod.AddNewPort("p1", PortDirection.Input);
        Port p2 = instanceMod.AddNewPort("p2", 2, PortDirection.Output);

        Instantiation instantiation = new(instanceMod, parentMod, "i1");

        Assert.AreEqual(parentMod, instantiation.ParentModule);
        Assert.AreEqual(instanceMod, instantiation.InstantiatedModule);
        Assert.AreEqual("i1", instantiation.Name);
        Assert.AreEqual("instanceMod in parentMod", instantiation.ToString());

        HashSet<IModuleLinkedSubcircuitDefinition> definitions = new([new ModuleLinkedSubcircuitDefinition(instanceMod, new EntityCollection(), [])]);
        Assert.ThrowsException<IncompleteException>(() => instantiation.GetSpice(definitions));
        Assert.ThrowsException<IncompleteException>(instantiation.GetVhdlStatement);
        definitions.Clear();

        Signal s1 = parentMod.GenerateSignal("s1");
        Vector v1 = parentMod.GenerateVector("v1", 2);
        instantiation.PortMapping.SetPort("p1", s1);
        instantiation.PortMapping.SetPort("p2", v1);
        instanceMod.SignalBehaviors[p2.Signal] = new ValueBehavior(3);

        parentMod.Instantiations.Add(instantiation);
        definitions.Add(instanceMod.GetSpice().AsModuleLinkedSubcircuit());
        string spice = instantiation.GetSpice(definitions).AsString();
        string expectedSpice = 
        """
        .subckt instanceMod p1 p2_0 p2_1
            .MODEL NmosMod nmos W=0.0001 L=1E-06
            .MODEL PmosMod pmos W=0.0001 L=1E-06
            VVDD VDD 0 5
            Vn0x0_value p2_0 0 5
            Vn0x1_value p2_1 0 5
            Rn1x0_floating p2_0 0 1000000000
            Rn2x1_floating p2_1 0 1000000000
        .ends instanceMod

        Xi1 s1 v1_0 v1_1 instanceMod
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check VHDL
        string vhdl = instantiation.GetVhdlStatement();
        string expectedVhdl =
        """
        i1 : instanceMod
            port map (
                p1 => s1,
                p2 => v1
            );
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check module Spice
        parentMod.AddNewPort(s1, PortDirection.Input);
        parentMod.AddNewPort(v1, PortDirection.Output);
        spice = parentMod.GetSpice().AsString();
        expectedSpice = 
        """
        .subckt instanceMod p1 p2_0 p2_1
            VVDD VDD 0 5
            Vn0x0_value p2_0 0 5
            Vn0x1_value p2_1 0 5
            Rn1x0_floating p2_0 0 1000000000
            Rn2x1_floating p2_1 0 1000000000
        .ends instanceMod

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        Xi1 s1 v1_0 v1_1 instanceMod
        Rn0x0_floating v1_0 0 1000000000
        Rn1x1_floating v1_1 0 1000000000
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check module VHDL
        vhdl = parentMod.GetVhdl();
        expectedVhdl =
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity parentMod is
            port (
                s1	: in	std_logic;
                v1	: out	std_logic_vector(1 downto 0)
            );
        end parentMod;

        architecture rtl of parentMod is
        component instanceMod
            port (
                p1	: in	std_logic;
                p2	: out	std_logic_vector(1 downto 0)
            );
        end component instanceMod;

        begin
            i1 : instanceMod
                port map (
                    p1 => s1,
                    p2 => v1
                );
            
        end rtl;

        entity instanceMod is
            port (
                p1	: in	std_logic;
                p2	: out	std_logic_vector(1 downto 0)
            );
        end instanceMod;

        architecture rtl of instanceMod is
        begin
            p2 <= "11";
        end rtl;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check simulation rule and its output values
        SimulationRule simRule = parentMod.GetSimulationRules().First();
        SubcircuitReference parentModRef = new(parentMod, []);
        SubcircuitReference instanceRef = parentModRef.GetChildSubcircuitReference(instantiation);
        SignalReference v1Ref = new(parentModRef, v1);
        SignalReference p2Ref = instanceRef.GetChildSignalReference(p2.Signal);
        Assert.AreEqual(v1Ref.Ascend(), simRule.OutputSignal.Ascend());
        Assert.AreEqual(p2Ref.Ascend(), simRule.OutputSignal.Ascend());
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint([], [0, 1], 1);
        int value = simRule.OutputValueCalculation(state);
        int expectedValue = 3;
        Assert.AreEqual(expectedValue, value);
    }

    [TestMethod]
    public void MultiInstanceTest()
    {
        Module parentMod = new("parentMod");
        Module andMod = Util.GetAndModule();
        Module orMod = Util.GetOrModule();
        Signal in1 = parentMod.GenerateSignal("in1");
        Signal in2 = parentMod.GenerateSignal("in2");
        Signal in3 = parentMod.GenerateSignal("in3");
        Signal out1 = parentMod.GenerateSignal("out1");
        Signal out2 = parentMod.GenerateSignal("out2");
        Signal out3 = parentMod.GenerateSignal("out3");

        parentMod.AddNewPort(in1, PortDirection.Input);
        parentMod.AddNewPort(in2, PortDirection.Input);
        parentMod.AddNewPort(in3, PortDirection.Input);

        Instantiation instance1 = parentMod.AddNewInstantiation(andMod, "and1");
        instance1.PortMapping.SetPort("IN1", in1);
        instance1.PortMapping.SetPort("IN2", in2);
        instance1.PortMapping.SetPort("OUT", out1);

        Instantiation instance2 = parentMod.AddNewInstantiation(andMod, "and2");
        instance2.PortMapping.SetPort("IN1", in2);
        instance2.PortMapping.SetPort("IN2", in3);
        instance2.PortMapping.SetPort("OUT", out2);

        Instantiation instance3 = parentMod.AddNewInstantiation(orMod, "or1");
        instance3.PortMapping.SetPort("IN1", out1);
        instance3.PortMapping.SetPort("IN2", out2);
        instance3.PortMapping.SetPort("OUT", out3);

        string spice = parentMod.Instantiations.GetSpice().AsString();
        string expectedSpice = 
        $"""
        .subckt AndMod IN1 IN2 OUT
        {Util.GetAndSubcircuitSpice(2, false).AddIndentation(1)}
            .MODEL NmosMod nmos W=0.0001 L=1E-06
            .MODEL PmosMod pmos W=0.0001 L=1E-06
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
            Rn0x0_connect n0_0x0_andout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends AndMod

        .subckt OrMod IN1 IN2 OUT
        {Util.GetOrSubcircuitSpice(2, false).AddIndentation(1)}
            .MODEL NmosMod nmos W=0.0001 L=1E-06
            .MODEL PmosMod pmos W=0.0001 L=1E-06
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Xn0_0x0_or n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_orout OR2
            Rn0x0_connect n0_0x0_orout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends OrMod

        Xand1 in1 in2 out1 AndMod
        Xand2 in2 in3 out2 AndMod
        Xor1 out1 out2 out3 OrMod
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));
        spice = parentMod.GetSpice().AsString();
        expectedSpice = 
        $"""
        .subckt AndMod IN1 IN2 OUT
        {Util.GetAndSubcircuitSpice(2, false).AddIndentation(1)}
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
            Rn0x0_connect n0_0x0_andout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends AndMod

        .subckt OrMod IN1 IN2 OUT
        {Util.GetOrSubcircuitSpice(2, false).AddIndentation(1)}    
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Xn0_0x0_or n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_orout OR2
            Rn0x0_connect n0_0x0_orout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends OrMod

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        Xand1 in1 in2 out1 AndMod
        Xand2 in2 in3 out2 AndMod
        Xor1 out1 out2 out3 OrMod
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check rules
        SimulationRule[] rules = [.. parentMod.GetSimulationRules()];
        SubcircuitReference parentModRef = new(parentMod, []);
        Assert.AreEqual(3, rules.Length);
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == parentModRef.GetChildSignalReference(out1)));
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == parentModRef.GetChildSignalReference(out2)));
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == parentModRef.GetChildSignalReference(out3)));
    }

    [TestMethod]
    public void HierarchicalInstanceTest()
    {
        Module parentMod = new("parentMod");
        Module andMod = Util.GetAndModule();
        Signal in1 = parentMod.GenerateSignal("in1");
        Signal in2 = parentMod.GenerateSignal("in2");
        Signal in3 = parentMod.GenerateSignal("in3");
        Signal out1 = parentMod.GenerateSignal("out1");
        Signal out2 = parentMod.GenerateSignal("out2");

        parentMod.AddNewPort(in1, PortDirection.Input);
        parentMod.AddNewPort(in2, PortDirection.Input);
        parentMod.AddNewPort(in3, PortDirection.Input);
        
        Module middleMod = new("middle");
        Port midPIn = middleMod.AddNewPort("IN", PortDirection.Input);
        Port midPOut = middleMod.AddNewPort("OUT", PortDirection.Output);

        Instantiation and1 = parentMod.AddNewInstantiation(andMod, "and1");
        and1.PortMapping.SetPort("IN1", in1);
        and1.PortMapping.SetPort("IN2", in2);
        and1.PortMapping.SetPort("OUT", out1);

        Instantiation middleAnd1 = middleMod.AddNewInstantiation(andMod, "middleAnd");
        middleAnd1.PortMapping.SetPort("IN1", midPIn.Signal);
        middleAnd1.PortMapping.SetPort("IN2", midPIn.Signal);
        middleAnd1.PortMapping.SetPort("OUT", midPOut.Signal);

        Instantiation instMiddle = parentMod.AddNewInstantiation(middleMod, "middleInst");
        instMiddle.PortMapping.SetPort("IN", in3);
        instMiddle.PortMapping.SetPort("OUT", out2);

        string spice = parentMod.Instantiations.GetSpice().AsString();
        string expectedSpice = 
        $"""
        .subckt AndMod IN1 IN2 OUT
        {Util.GetAndSubcircuitSpice(2, false).AddIndentation(1)}
            .MODEL NmosMod nmos W=0.0001 L=1E-06
            .MODEL PmosMod pmos W=0.0001 L=1E-06
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
        	Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
            Rn0x0_connect n0_0x0_andout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends AndMod

        .subckt middle IN OUT
            .MODEL NmosMod nmos W=0.0001 L=1E-06
            .MODEL PmosMod pmos W=0.0001 L=1E-06
            VVDD VDD 0 5
            XmiddleAnd IN IN OUT AndMod
            Rn0x0_floating OUT 0 1000000000
        .ends middle

        Xand1 in1 in2 out1 AndMod
        XmiddleInst in3 out2 middle
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));
        
        // Check rules
        SimulationRule[] rules = [.. parentMod.GetSimulationRules()];
        SubcircuitReference parentModRef = new(parentMod, []);
        Assert.AreEqual(2, rules.Length);
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == parentModRef.GetChildSignalReference(out1)));
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == parentModRef.GetChildSignalReference(out2)));
    }

    [TestMethod]
    public void HierarchicalInstanceTestWhereInnerModuleIsUsedByAnotherSubcircuit()
    {
        Module parentMod = new("parentMod");
        Module andMod = Util.GetAndModule();
        Signal in1 = parentMod.GenerateSignal("in1");
        Signal in2 = parentMod.GenerateSignal("in2");
        Signal out1 = parentMod.GenerateSignal("out1");
        Signal out2 = parentMod.GenerateSignal("out2");

        parentMod.AddNewPort(in1, PortDirection.Input);
        parentMod.AddNewPort(in2, PortDirection.Input);
        
        Module middle1Mod = new("middle1");
        Port mid1PIn = middle1Mod.AddNewPort("IN", PortDirection.Input);
        Port mid1POut = middle1Mod.AddNewPort("OUT", PortDirection.Output);
        
        Module middle2Mod = new("middle2");
        Port mid2PIn = middle2Mod.AddNewPort("IN", PortDirection.Input);
        Port mid2POut = middle2Mod.AddNewPort("OUT", PortDirection.Output);

        Instantiation instMiddle1 = parentMod.AddNewInstantiation(middle1Mod, "middle1Inst");
        instMiddle1.PortMapping.SetPort("IN", in1);
        instMiddle1.PortMapping.SetPort("OUT", out1);

        Instantiation instMiddle2 = parentMod.AddNewInstantiation(middle2Mod, "middle2Inst");
        instMiddle2.PortMapping.SetPort("IN", in2);
        instMiddle2.PortMapping.SetPort("OUT", out2);

        Instantiation middle1And = middle1Mod.AddNewInstantiation(andMod, "middle1And");
        middle1And.PortMapping.SetPort("IN1", mid1PIn.Signal);
        middle1And.PortMapping.SetPort("IN2", mid1PIn.Signal);
        middle1And.PortMapping.SetPort("OUT", mid1POut.Signal);

        Instantiation middle2And = middle2Mod.AddNewInstantiation(andMod, "middle2And");
        middle2And.PortMapping.SetPort("IN1", mid2PIn.Signal);
        middle2And.PortMapping.SetPort("IN2", mid2PIn.Signal);
        middle2And.PortMapping.SetPort("OUT", mid2POut.Signal);

        // Confirm that the AND used by both subcircuits is the same
        SpiceSubcircuit moduleSubcircuit = parentMod.GetSpice();
        ISubcircuitDefinition definition = moduleSubcircuit.AsSubcircuit();
        Subcircuit middle1InstEntity = definition.Entities.First(e => e.Name == "middle1Inst") as Subcircuit ?? throw new Exception("Should be subcircuit");
        Subcircuit middle2InstEntity = definition.Entities.First(e => e.Name == "middle2Inst") as Subcircuit ?? throw new Exception("Should be subcircuit");
        Subcircuit middle1AndEntity = middle1InstEntity.Parameters.Definition.Entities.First(e => e.Name == "middle1And") as Subcircuit ?? throw new Exception("Should be subcircuit");
        Subcircuit middle2AndEntity = middle2InstEntity.Parameters.Definition.Entities.First(e => e.Name == "middle2And") as Subcircuit ?? throw new Exception("Should be subcircuit");
        Assert.AreEqual(middle1AndEntity.Parameters.Definition, middle2AndEntity.Parameters.Definition);

        // Since AND isn't used at the top level, it is declared in both middle subcircuits
        string spice = moduleSubcircuit.AsString();
        string expectedSpice = 
        $"""
        .subckt middle1 IN OUT
            .subckt AndMod IN1 IN2 OUT
        {Util.GetAndSubcircuitSpice(2, false).AddIndentation(2)}
                VVDD VDD 0 5
                Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
                Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
        		Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
                Rn0x0_connect n0_0x0_andout OUT 0.001
                Rn1x0_floating OUT 0 1000000000
            .ends AndMod
            
            VVDD VDD 0 5
            Xmiddle1And IN IN OUT AndMod
            Rn0x0_floating OUT 0 1000000000
        .ends middle1

        .subckt middle2 IN OUT
            .subckt AndMod IN1 IN2 OUT
        {Util.GetAndSubcircuitSpice(2, false).AddIndentation(2)}
                
                VVDD VDD 0 5
                Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
                Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
        		Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
                Rn0x0_connect n0_0x0_andout OUT 0.001
                Rn1x0_floating OUT 0 1000000000
            .ends AndMod
            
            VVDD VDD 0 5
            Xmiddle2And IN IN OUT AndMod
            Rn0x0_floating OUT 0 1000000000
        .ends middle2

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        Xmiddle1Inst in1 out1 middle1
        Xmiddle2Inst in2 out2 middle2
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));
    }

    [TestMethod]
    public void CollectionWithDuplicateModuleTest()
    {
        Module parentMod = new("parentMod");
        Signal s1 = parentMod.GenerateSignal("s1");
        Signal s2 = parentMod.GenerateSignal("s2");
        Vector v3 = parentMod.GenerateVector("v3", 2);
        Signal s4 = parentMod.GenerateSignal("s4");

        Module instanceMod = Util.GetSampleModule1();
        IPort i1p1 = instanceMod.Ports.ElementAt(0);
        IPort i1p2 = instanceMod.Ports.ElementAt(1);

        Module instanceMod2 = Util.GetSampleModule2();
        IPort i2p1 = instanceMod2.Ports.ElementAt(0);
        IPort i2p2 = instanceMod2.Ports.ElementAt(1);

        Instantiation i1 = new(instanceMod, parentMod, "i1");
        Instantiation i2 = new(instanceMod, parentMod, "i2");
        Instantiation i3 = new(instanceMod2, parentMod, "i3");

        i1.PortMapping[i1p1] = s1;
        i1.PortMapping[i1p2] = s2;
        
        i2.PortMapping[i1p1] = s1;
        i2.PortMapping[i1p2] = s4;

        i3.PortMapping[i2p1] = s1;
        i3.PortMapping[i2p2] = v3;

        parentMod.Instantiations.Add(i1);
        parentMod.Instantiations.Add(i2);
        parentMod.Instantiations.Add(i3);

        IEnumerable<Subcircuit> spiceInstantiations = parentMod.Instantiations.GetSpice().AsCircuit().OfType<Subcircuit>();
        Assert.AreEqual(2, spiceInstantiations.Select(i => i.Parameters.Definition).Distinct().Count());
    }
}