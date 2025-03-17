using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Behaviors;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

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

        Dictionary<IModule, SubcircuitDefinition> definitions = new()
        {
            {instanceMod, new SubcircuitDefinition(new EntityCollection())}
        };
        Assert.ThrowsException<IncompleteException>(() => instantiation.GetSpice(definitions));
        Assert.ThrowsException<IncompleteException>(instantiation.GetVhdlStatement);

        Signal s1 = parentMod.GenerateSignal("s1");
        Vector v1 = parentMod.GenerateVector("v1", 2);
        instantiation.PortMapping.SetPort("p1", s1);
        instantiation.PortMapping.SetPort("p2", v1);
        instanceMod.SignalBehaviors[p2.Signal] = new ValueBehavior(3);

        parentMod.Instantiations.Add(instantiation);
        definitions[instanceMod] = instanceMod.GetSpice().AsSubcircuit();
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

        // Check module Spice
        spice = parentMod.GetSpice().AsString();
        expectedSpice = 
        """
        .subckt subcircuit0 p1 p2_0 p2_1
            VVDD VDD 0 5
            Vn0x0_value p2_0 0 5
            Vn0x1_value p2_1 0 5
            Rn1x0_floating p2_0 0 1000000000
            Rn2x1_floating p2_1 0 1000000000
        .ends subcircuit0

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        Xi1 s1 v1_0 v1_1 subcircuit0
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));
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
        """
        .subckt AND IN1 IN2 OUT
            .MODEL NmosMod nmos W=0.0001 L=1E-06
            .MODEL PmosMod pmos W=0.0001 L=1E-06
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Mn0_0x0_pnor0 n0_0x0_norout n0_0_0x0_baseout n0_0x0_nor1 n0_0x0_nor1 PmosMod
            Mn0_0x0_nnor0 n0_0x0_norout n0_0_0x0_baseout 0 0 NmosMod
            Mn0_0x0_pnor1 n0_0x0_nor1 n0_0_1x0_baseout VDD VDD PmosMod
            Mn0_0x0_nnor1 n0_0x0_norout n0_0_1x0_baseout 0 0 NmosMod
            Mn0_0x0_pnot n0_0x0_orout n0_0x0_norout VDD VDD PmosMod
            Mn0_0x0_nnot n0_0x0_orout n0_0x0_norout 0 0 NmosMod
            Rn0x0_connect n0_0x0_orout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends AND

        .subckt OR IN1 IN2 OUT
            .MODEL NmosMod nmos W=0.0001 L=1E-06
            .MODEL PmosMod pmos W=0.0001 L=1E-06
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Mn0_0x0_pnand0 n0_0x0_nandout n0_0_0x0_baseout VDD VDD PmosMod
            Mn0_0x0_nnand0 n0_0x0_nandout n0_0_0x0_baseout n0_0x0_nand1 n0_0x0_nand1 NmosMod
            Mn0_0x0_pnand1 n0_0x0_nandout n0_0_1x0_baseout VDD VDD PmosMod
            Mn0_0x0_nnand1 n0_0x0_nand1 n0_0_1x0_baseout 0 0 NmosMod
            Mn0_0x0_pnot n0_0x0_andout n0_0x0_nandout VDD VDD PmosMod
            Mn0_0x0_nnot n0_0x0_andout n0_0x0_nandout 0 0 NmosMod
            Rn0x0_connect n0_0x0_andout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends OR

        Xand1 in1 in2 out1 AND
        Xand2 in2 in3 out2 AND
        Xor1 out1 out2 out3 OR
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));
        spice = parentMod.GetSpice().AsString();
        expectedSpice = 
        """
        .subckt subcircuit0 IN1 IN2 OUT
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Mn0_0x0_pnor0 n0_0x0_norout n0_0_0x0_baseout n0_0x0_nor1 n0_0x0_nor1 PmosMod
            Mn0_0x0_nnor0 n0_0x0_norout n0_0_0x0_baseout 0 0 NmosMod
            Mn0_0x0_pnor1 n0_0x0_nor1 n0_0_1x0_baseout VDD VDD PmosMod
            Mn0_0x0_nnor1 n0_0x0_norout n0_0_1x0_baseout 0 0 NmosMod
            Mn0_0x0_pnot n0_0x0_orout n0_0x0_norout VDD VDD PmosMod
            Mn0_0x0_nnot n0_0x0_orout n0_0x0_norout 0 0 NmosMod
            Rn0x0_connect n0_0x0_orout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends subcircuit0

        .subckt subcircuit0 IN1 IN2 OUT
            VVDD VDD 0 5
            Rn0_0_0x0_res IN1 n0_0_0x0_baseout 0.001
            Rn0_0_1x0_res IN2 n0_0_1x0_baseout 0.001
            Mn0_0x0_pnand0 n0_0x0_nandout n0_0_0x0_baseout VDD VDD PmosMod
            Mn0_0x0_nnand0 n0_0x0_nandout n0_0_0x0_baseout n0_0x0_nand1 n0_0x0_nand1 NmosMod
            Mn0_0x0_pnand1 n0_0x0_nandout n0_0_1x0_baseout VDD VDD PmosMod
            Mn0_0x0_nnand1 n0_0x0_nand1 n0_0_1x0_baseout 0 0 NmosMod
            Mn0_0x0_pnot n0_0x0_andout n0_0x0_nandout VDD VDD PmosMod
            Mn0_0x0_nnot n0_0x0_andout n0_0x0_nandout 0 0 NmosMod
            Rn0x0_connect n0_0x0_andout OUT 0.001
            Rn1x0_floating OUT 0 1000000000
        .ends subcircuit0

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        Xand1 in1 in2 out1 subcircuit0
        Xand2 in2 in3 out2 subcircuit0
        Xor1 out1 out2 out3 subcircuit0
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));
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
        
        Module middleInstanceMod = new("middle");
        Port midPIn = middleInstanceMod.AddNewPort("IN", PortDirection.Input);
        Port midPOut = middleInstanceMod.AddNewPort("OUT", PortDirection.Output);

        Instantiation and1 = parentMod.AddNewInstantiation(andMod, "and1");
        and1.PortMapping.SetPort("IN1", in1);
        and1.PortMapping.SetPort("IN2", in2);
        and1.PortMapping.SetPort("OUT", out1);

        Instantiation middleAnd1 = middleInstanceMod.AddNewInstantiation(andMod, "middleAnd");
        middleAnd1.PortMapping.SetPort("IN1", midPIn.Signal);
        middleAnd1.PortMapping.SetPort("IN2", midPIn.Signal);
        middleAnd1.PortMapping.SetPort("OUT", midPOut.Signal);

        Instantiation instMiddle = parentMod.AddNewInstantiation(middleInstanceMod, "middleAndInst");
        instMiddle.PortMapping.SetPort("IN", in3);
        instMiddle.PortMapping.SetPort("OUT", out2);

        string spice = parentMod.Instantiations.GetSpice().AsString();
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

        IEnumerable<Subcircuit> spiceInstantiations = parentMod.Instantiations.GetSpice().AsCircuit().Where(e => e is Subcircuit).Select(e => (Subcircuit)e);
        Assert.AreEqual(2, spiceInstantiations.Select(i => i.Parameters.Definition).Distinct().Count());
    }
}