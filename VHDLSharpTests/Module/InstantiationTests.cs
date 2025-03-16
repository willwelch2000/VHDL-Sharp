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