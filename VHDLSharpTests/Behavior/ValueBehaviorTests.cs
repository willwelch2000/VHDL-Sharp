using VHDLSharp.Behaviors;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class ValueBehaviorTests
{
    [TestMethod]
    public void SingleDimensionTest()
    {
        Module module1 = new("module1");
        Signal s1 = module1.GenerateSignal("s1");
        Vector v2 = module1.GenerateVector("v2", 3);
        ValueBehavior behavior = new(1);
        
        Assert.AreEqual(1, behavior.Value);
        Assert.AreEqual(new Dimension(1, null), behavior.Dimension);
        Assert.IsNull(behavior.ParentModule);
        Assert.IsFalse(behavior.InputModuleSignals.Any());

        // Compatibility
        Assert.IsTrue(behavior.IsCompatible(s1));
        Assert.IsTrue(behavior.IsCompatible(v2));

        // Check Spice
        string spice = behavior.GetSpice(s1, "0").AsString();
        string expectedSpice = 
        """
        Vn0x0_value s1 0 5
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check VHDL
        string vhdl = behavior.GetVhdlStatement(s1);
        string expectedVhdl = "s1 <= \"1\";";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));
        
        // Check simulation rule and its output values
        SubcircuitReference subcircuitRef = new(module1, []);
        SignalReference s1Ref = new(subcircuitRef, s1);
        SignalReference v2Ref = new(subcircuitRef, v2);
        SimulationRule simRule = behavior.GetSimulationRule(v2Ref);
        Assert.AreEqual(v2Ref, simRule.OutputSignal);
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        for (int s1Val = 0; s1Val < 2; s1Val++)
        {
            RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint(new()
            {
                {s1Ref, [s1Val]},
            }, [0, 1], 1);
            int value = simRule.OutputValueCalculation(state);
            int expectedValue = 1;
            Assert.AreEqual(expectedValue, value);
        }
    }

    [TestMethod]
    public void MultiDimensionTest()
    {
        Module module1 = new("module1");
        Signal s1 = module1.GenerateSignal("s1");
        Vector v2 = module1.GenerateVector("v2", 4);
        Vector v3 = module1.GenerateVector("v3", 3);
        ValueBehavior behavior1 = new(10);
        ValueBehavior behavior2 = new(6);

        // Basic stuff
        Assert.AreEqual(10, behavior1.Value);
        Assert.AreEqual(6, behavior2.Value);
        Assert.AreEqual(new Dimension(4, null), behavior1.Dimension);
        Assert.AreEqual(new Dimension(3, null), behavior2.Dimension);
        Assert.IsNull(behavior1.ParentModule);
        Assert.IsNull(behavior2.ParentModule);
        Assert.IsFalse(behavior1.InputModuleSignals.Any());
        Assert.IsFalse(behavior2.InputModuleSignals.Any());

        // Compatibility
        Assert.IsFalse(behavior1.IsCompatible(s1));
        Assert.IsFalse(behavior2.IsCompatible(s1));
        Assert.IsTrue(behavior1.IsCompatible(v2));
        Assert.IsTrue(behavior2.IsCompatible(v2));
        Assert.IsFalse(behavior1.IsCompatible(v3));
        Assert.IsTrue(behavior2.IsCompatible(v3));

        // Check Spice
        string spice = behavior1.GetSpice(v2, "0").AsString();
        string expectedSpice = 
        """
        Vn0x0_value v2_0 0 0
        Vn0x1_value v2_1 0 5
        Vn0x2_value v2_2 0 0
        Vn0x3_value v2_3 0 5
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check VHDL
        string vhdl = behavior1.GetVhdlStatement(v2);
        string expectedVhdl = "v2 <= \"1010\";";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));
        
        // Check simulation rule and its output values
        SubcircuitReference subcircuitRef = new(module1, []);
        SignalReference v2Ref = new(subcircuitRef, v2);
        SimulationRule simRule = behavior1.GetSimulationRule(v2Ref);
        Assert.AreEqual(v2Ref, simRule.OutputSignal);
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint([], [0, 1], 1);
        int value = simRule.OutputValueCalculation(state);
        int expectedValue = 10;
        Assert.AreEqual(expectedValue, value);
    }
}