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

        Assert.ThrowsException<Exception>(instantiation.GetSpice);
        // Assert.ThrowsException<Exception>(() => instantiation.GetSpiceSharpSubcircuit);
        Assert.ThrowsException<Exception>(instantiation.GetVhdlStatement);
    }
}