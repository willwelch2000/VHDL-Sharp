using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class VectorSliceTests
{
    [TestMethod]
    public void BasicTest()
    {
        Module module = new("m1");
        Vector vector = new("v1", module, 4);
        VectorSlice slice = vector[2..4];

        Assert.AreEqual(slice.Vector, vector);
        Assert.AreEqual(2, slice.StartNode);
        Assert.AreEqual(4, slice.EndNode);
        Assert.AreEqual(2, slice.Dimension.NonNullValue);
        Assert.AreEqual(vector[2], slice[0]);
        Assert.AreEqual(vector[3], slice[1]);
        Assert.AreEqual("v1[2:4]", slice.Name);
        Assert.AreEqual(slice.ParentSignal, vector);
        Assert.AreEqual(slice.ParentModule, module);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => vector[-1..3]);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => vector[2..5]);
    }

    [TestMethod]
    public void CanCombineTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        Vector v1 = new("v1", module1, 4);
        Vector v2 = new("v2", module1, 4);
        Vector v3 = new("v3", module2, 4);
        Vector v4 = new("v4", module1, 2);
        Signal s1 = new("s1", module1);
        Literal literal1 = new(1, 2);
        Literal literal2 = new(1, 1);

        VectorSlice slice1 = v1[1..3];
        VectorSlice slice2 = v2[1..3];
        VectorSlice slice3 = v3[1..3];
        VectorSlice slice4 = v1[1..2];

        Assert.IsTrue(slice1.CanCombine(slice2));
        Assert.IsTrue(slice1.CanCombine(v4));
        Assert.IsTrue(slice1.CanCombine(literal1));
        Assert.IsFalse(slice1.CanCombine(slice3));
        Assert.IsFalse(slice1.CanCombine(slice4));
        Assert.IsFalse(slice1.CanCombine(literal2));
        Assert.IsTrue(slice4.CanCombine(literal2));
        Assert.IsFalse(slice1.CanCombine(s1));
        Assert.IsTrue(slice4.CanCombine(s1));
        Assert.IsTrue(slice1.CanCombine([slice2, literal1]));
        // False because they come from different modules
        Assert.IsFalse(slice1.CanCombine([literal1, slice3]));
    }

    [TestMethod]
    public void IsPartOfPortMappingTest()
    {
        Module topMod = new("top");
        Vector v1 = topMod.GenerateVector("v1", 5);
        VectorSlice v1Slice = v1[2..5];
        Vector v2 = topMod.GenerateVector("v2", 5);
        VectorSlice v2Slice = v2[2..5];
        Module instMod = new("inst");
        Port pIn1 = instMod.AddNewPort("in", 3, PortDirection.Input);
        Port pIn2 = instMod.AddNewPort("in", 5, PortDirection.Input);
        INamedSignal pIn2Slice = pIn2.Signal[2..5];
        Instantiation inst = topMod.AddNewInstantiation(instMod, "inst1");
        inst.PortMapping[pIn1] = v1Slice;

        // Port mapped to slice
        bool partOfPM = pIn1.Signal.IsPartOfPortMapping(inst.PortMapping, out INamedSignal? equivalentSignal);
        Assert.IsTrue(partOfPM);
        Assert.AreEqual(v1Slice, equivalentSignal);

        // Slice not in mapping
        partOfPM = pIn2Slice.IsPartOfPortMapping(inst.PortMapping, out _);
        Assert.IsFalse(partOfPM);

        // Slice in mapping
        inst.PortMapping[pIn2] = v2;
        partOfPM = pIn2Slice.IsPartOfPortMapping(inst.PortMapping, out equivalentSignal);
        Assert.IsTrue(partOfPM);
        Assert.IsTrue(v2Slice.Equals(equivalentSignal));
    }
}