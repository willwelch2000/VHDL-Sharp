using VHDLSharp.BuiltIn;
using VHDLSharp.Modules;

namespace VHDLSharpTests;

[TestClass]
public class ParameterizedModuleTests
{
    [TestMethod]
    // Tests that parameterized modules evaluate correctly as equal regardless of the type it's called
    public void EqualityTest()
    {
        DFlipFlop dff1 = new(new());
        DFlipFlop dff2 = new(new() { NegativeEdgeTriggered = true });
        DFlipFlop dff3 = new(new());
        Assert.IsFalse(dff1.Equals(dff2));
        Assert.IsTrue(dff1.Equals(dff3));

        ParameterizedModule<DFlipFlopParams> dff1p = dff1;
        ParameterizedModule<DFlipFlopParams> dff2p = dff2;
        ParameterizedModule<DFlipFlopParams> dff3p = dff3;
        Assert.IsFalse(dff1p.Equals(dff2p));
        Assert.IsTrue(dff1p.Equals(dff3p));

        IModule dff1i = dff1;
        IModule dff2i = dff2;
        IModule dff3i = dff3;
        Assert.IsFalse(dff1i.Equals(dff2i));
        Assert.IsTrue(dff1i.Equals(dff3i));
    }
}