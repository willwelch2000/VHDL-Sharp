using SpiceSharp.Components;
using SpiceSharp.Entities;
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
        instanceMod.AddNewPort("p1", PortDirection.Input);
        instanceMod.AddNewPort("p2", 2, PortDirection.Output);

        Instantiation instantiation = new(instanceMod, parentMod, "i1");

        Assert.AreEqual(parentMod, instantiation.ParentModule);
        Assert.AreEqual(instanceMod, instantiation.InstantiatedModule);
        Assert.AreEqual("i1", instantiation.Name);
        Assert.AreEqual("Xi1", instantiation.SpiceName);
        Assert.AreEqual("instanceMod in parentMod", instantiation.ToString());

        Assert.ThrowsException<IncompleteException>(instantiation.GetSpice);
        Dictionary<IModule, SubcircuitDefinition> definitions = new()
        {
            {instanceMod, new SubcircuitDefinition(new EntityCollection())}
        };
        Assert.ThrowsException<IncompleteException>(() => instantiation.GetSpiceSharpSubcircuit(definitions));
        Assert.ThrowsException<IncompleteException>(instantiation.GetVhdlStatement);
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

        IEnumerable<Subcircuit> spiceInstantiations = parentMod.Instantiations.GetSpiceSharpEntities().Where(e => e is Subcircuit).Select(e => (Subcircuit)e);
        Assert.AreEqual(2, spiceInstantiations.Select(i => i.Parameters.Definition).Distinct().Count());
    }
}