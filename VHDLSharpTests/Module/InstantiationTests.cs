using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;

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

        Module instanceMod = TestUtil.GetSampleModule1();

        Module instanceMod2 = TestUtil.GetSampleModule2();

        Instantiation i1 = new(instanceMod, parentMod, "i1");
        Instantiation i2 = new(instanceMod, parentMod, "i2");
        Instantiation i3 = new(instanceMod2, parentMod, "i3");

        parentMod.Instantiations.Add(i1);
        parentMod.Instantiations.Add(i2);
        parentMod.Instantiations.Add(i3);

        IEnumerable<Subcircuit> spiceInstantiations = parentMod.Instantiations.GetSpiceSharpEntities().Where(e => e is Subcircuit).Select(e => (Subcircuit)e);
        Assert.AreEqual(2, spiceInstantiations.Select(i => i.Parameters.Definition).Count());
    }
}