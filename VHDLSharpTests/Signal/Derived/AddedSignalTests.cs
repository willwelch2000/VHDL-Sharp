using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class AddedSignalTests
{
    [TestMethod]
    public void BasicTest()
    {
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Signal s2 = module.GenerateSignal("s2");
        ISignal s3 = s1.Plus(s2);
    }
}