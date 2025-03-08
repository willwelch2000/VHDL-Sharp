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
        // TODO next
    }
}