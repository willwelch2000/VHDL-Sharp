using VHDLSharp.Modules;
using VHDLSharp.Signals;
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
        Module instance = new("instance");
        PortMapping mapping = new(instance, parent);
        ValidityManager.MonitorMode = MonitorMode.AlertUpdates;
        ((IValidityManagedEntity)mapping).Updated += (s, e) => callbackCount++;
        ((IValidityManagedEntity)mapping).ValidityManager.ChangeDetectedInMainOrTrackedEntity += (s, e) => childCallbackCount++;

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

    [TestMethod]
    public void InvalidOperationTest()
    {
        Module parent = new("parent");
        Module instance = new("instance");
        PortMapping mapping = new(instance, parent);
        Vector parentV1 = parent.GenerateVector("v1", 3);
        Vector parentV2 = parent.GenerateVector("v2", 2);
        Port instanceP1 = instance.AddNewPort("p1", 2, PortDirection.Output);
        ValidityManager.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;

        // Incompatible signals
        Assert.ThrowsException<PortMappingException>(() => mapping[instanceP1] = parentV1);
        Issue issue = ((IValidityManagedEntity)mapping).ValidityManager.Issues().First();
        Assert.AreEqual(0, issue.FaultChain.Count);
        Assert.AreEqual("Port p1 and signal v1 must have the same dimension", issue.Exception.Message);
        mapping.Remove(instanceP1);

        mapping[instanceP1] = parentV2;
        // Make v2 an input port in parent, confirm that causes error
        Assert.ThrowsException<PortMappingException>(() => parent.AddNewPort(parentV2, PortDirection.Input));
    }
}