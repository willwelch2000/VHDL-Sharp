using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class ValidityManagerTests
{
    [TestMethod]
    public void DoubleValidationCheck()
    {
        TestNode node1 = new();
        TestNode node2 = new();
        TestNode node3 = new();

        int node1Count = 0;
        int node2Count = 0;
        int node3Count = 0;

        node1.ThisOrTrackedEntityUpdated += (s, e) => node1Count++;
        node2.ThisOrTrackedEntityUpdated += (s, e) => node2Count++;
        node3.ThisOrTrackedEntityUpdated += (s, e) => node3Count++;

        node1.TrackedEntities.Add(node2);
        node2.TrackedEntities.Add(node3);

        node3.InvokeUpdated();
        Assert.AreEqual(1, node1Count);
        Assert.AreEqual(1, node2Count);
        Assert.AreEqual(1, node3Count);

        // If this is not working, node1 will update twice instead of just once
        node1.TrackedEntities.Add(node3);
        node3.InvokeUpdated();
        Assert.AreEqual(2, node1Count);
        Assert.AreEqual(2, node2Count);
        Assert.AreEqual(2, node3Count);

        // Create full circle and test
        node3.TrackedEntities.Add(node1);
        node3.InvokeUpdated();
        Assert.AreEqual(3, node1Count);
        Assert.AreEqual(3, node2Count);
        Assert.AreEqual(3, node3Count);
    }
}