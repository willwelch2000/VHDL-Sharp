using VHDLSharp.Modules;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class PortMappingTests
{
    [TestMethod]
    public void CallbackTest()
    {
        int callbackCount = 0;
        int childCallbackCount = 0;
        Module parent = new("parent");
        // Port parentP2 = parent.AddNewPort("p2", PortDirection.Output);
        Module instance = new("instance");
        // Port instanceP1 = instance.AddNewPort("p1", PortDirection.Input);
        // Port instanceP2 = instance.AddNewPort("p2", PortDirection.Output);
        PortMapping mapping = new(instance, parent);
        ((IValidityManagedEntity)mapping).Updated += (s, e) => callbackCount++;
        ((IValidityManagedEntity)mapping).ValidityManager.ThisOrTrackedEntityUpdated += (s, e) => childCallbackCount++;

        Port parentP1 = parent.AddNewPort("p1", PortDirection.Input);
        Assert.AreEqual(0, callbackCount);
        Assert.AreEqual(1, childCallbackCount);

        Port instanceP1 = instance.AddNewPort("p1", PortDirection.Input);
        Assert.AreEqual(0, callbackCount);
        Assert.AreEqual(2, childCallbackCount);

        mapping[instanceP1] = parentP1.Signal;
        int a = callbackCount;
        Assert.AreEqual(1, callbackCount);
        Assert.AreEqual(3, childCallbackCount);
    }
}