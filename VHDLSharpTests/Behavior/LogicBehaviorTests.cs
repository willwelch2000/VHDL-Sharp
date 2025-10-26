using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Behaviors;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharpTests;

[TestClass]
public class LogicBehaviorTests
{
    private Signal? s1;
    private Signal? s2;
    private Signal? s3;

    private Module Module1 { get; } = new("m1");
    private Signal S1
    {
        get
        {
            s1 ??= new("s1", Module1);
            return s1;
        }
    }
    private Signal S2
    {
        get
        {
            s2 ??= new("s2", Module1);
            return s2;
        }
    }
    private Signal S3
    {
        get
        {
            s3 ??= new("s3", Module1);
            return s3;
        }
    }

    [TestMethod]
    public void AndExpressionTest()
    {
        Module module1 = Module1;
        Signal s1 = S1;
        Signal s2 = S2;
        Signal s3 = S3;

        LogicBehavior behavior = new(s1.And(s2));

        // Check Spice
        string spice = behavior.GetSpice(s3, "0").AsString();
        string expectedSpice = 
        $"""
        {Util.GetAndSubcircuitSpice(2, false)}
        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5

        Rn0_0_0x0_res s1 n0_0_0x0_baseout 0.001
        Rn0_0_1x0_res s2 n0_0_1x0_baseout 0.001
        Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
        Rn0x0_connect n0_0x0_andout s3 0.001
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check VHDL
        string vhdl = behavior.GetVhdlStatement(s3);
        string expectedVhdl = "s3 <= (s1 and s2);";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));

        // Check Spice# entities
        IEntity[] entities = [.. behavior.GetSpice(s3, "0").CircuitElements];
        Assert.AreEqual(entities.Length, 7);
        Mosfet1Model nmosMod = entities.First(e => e.Name == "NmosMod") as Mosfet1Model ?? throw new();
        Assert.IsTrue(nmosMod.Parameters.TypeName == "nmos" && nmosMod.Parameters.Width == 100e-6 && nmosMod.Parameters.Length == 1e-6);
        Mosfet1Model pmosMod = entities.First(e => e.Name == "PmosMod") as Mosfet1Model ?? throw new();
        Assert.IsTrue(pmosMod.Parameters.TypeName == "pmos" && pmosMod.Parameters.Width == 100e-6 && pmosMod.Parameters.Length == 1e-6);
        VoltageSource vddSource = entities.First(e => e.Name == "VDD") as VoltageSource ?? throw new();
        Assert.IsTrue(vddSource.Nodes.SequenceEqual(["VDD", "0"]) && vddSource.Parameters.DcValue == 5);
        Resistor resistor0 = entities.First(e => e.Name == "n0_0_0x0_res") as Resistor ?? throw new();
        Assert.IsTrue(resistor0.Nodes.SequenceEqual(["s1", "n0_0_0x0_baseout"]));
        Resistor resistor1 = entities.First(e => e.Name == "n0_0_1x0_res") as Resistor ?? throw new();
        Assert.IsTrue(resistor1.Nodes.SequenceEqual(["s2", "n0_0_1x0_baseout"]));
        Subcircuit andSubcircuit = entities.First(e => e.Name == "n0_0x0_and") as Subcircuit ?? throw new();
        Assert.IsTrue(andSubcircuit.Parameters.Definition is INamedSubcircuitDefinition namedDef && namedDef.Name == "AND2");
        Resistor resistorOut = entities.First(e => e.Name == "n0x0_connect") as Resistor ?? throw new();
        Assert.IsTrue(resistorOut.Nodes.SequenceEqual(["n0_0x0_andout", "s3"]));

        // Check named signals
        IModuleSpecificSignal[] behaviorSignals = [.. behavior.InputModuleSignals];
        Assert.AreEqual(2, behaviorSignals.Length);
        Assert.AreEqual(s1, behaviorSignals[0]);
        Assert.AreEqual(s2, behaviorSignals[1]);

        // Check simple stuff
        Assert.AreEqual(1, behavior.Dimension.Value);
        Assert.AreEqual(module1, behavior.ParentModule);

        // Check simulation rule and its output values
        SubcircuitReference subcircuitRef = new(module1, []);
        SignalReference s1Ref = new(subcircuitRef, s1);
        SignalReference s2Ref = new(subcircuitRef, s2);
        SignalReference s3Ref = new(subcircuitRef, s3);
        SimulationRule simRule = behavior.GetSimulationRule(s3Ref);
        Assert.AreEqual(s3Ref, simRule.OutputSignal);
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        for (int i = 0; i < 4; i++)
        {
            int s1Val = (i & 1) > 0 ? 1 : 0;
            int s2Val = (i & 2) > 0 ? 1 : 0;
            RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint(new()
            {
                {s1Ref, [s1Val]},
                {s2Ref, [s2Val]},
            }, [0, 1], 1);
            int value = simRule.OutputValueCalculation(state);
            int expectedValue = s1Val & s2Val;
            Assert.AreEqual(expectedValue, value);
        }
    }

    [TestMethod]
    public void ExceptionTests()
    {
        Module module1 = Module1;
        Signal s1 = S1;
        Signal s2 = S2;
        Signal s3 = S3;
        LogicBehavior behavior = new(s1.And(s2));

        Vector v4 = new("v4", module1, 2);
        Assert.IsTrue(behavior.IsCompatible(s3));
        Assert.IsFalse(behavior.IsCompatible(v4));
        Assert.ThrowsException<IncompatibleSignalException>(() => behavior.GetSpice(v4, "0"));
        Assert.ThrowsException<IncompatibleSignalException>(() => behavior.GetVhdlStatement(v4));
    }

    [TestMethod]
    public void MultiDimensionAndTest()
    {
        Module module1 = Module1;
        Vector v1 = new("v1", module1, 3);
        Vector v2 = new("v2", module1, 3);
        Vector v3 = new("v3", module1, 3);
        Vector v4 = new("v4", module1, 4);
        LogicBehavior behavior = new(v1.And(v2));

        // Check compatibility
        Assert.IsTrue(behavior.IsCompatible(v3));
        Assert.IsFalse(behavior.IsCompatible(v4));
        Assert.IsFalse(behavior.IsCompatible(S1));

        // Check dimension
        Assert.AreEqual(3, behavior.Dimension.Value);

        // Check VHDL
        string vhdl = behavior.GetVhdlStatement(v3);
        string expectedVhdl = "v3 <= (v1 and v2);";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));

        string spice = behavior.GetSpice(v3, "0").AsString();
        string expectedSpice = 
        $"""
        {Util.GetAndSubcircuitSpice(2, false)}
        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5

        Rn0_0_0x0_res v1_0 n0_0_0x0_baseout 0.001
        Rn0_0_0x1_res v1_1 n0_0_0x1_baseout 0.001
        Rn0_0_0x2_res v1_2 n0_0_0x2_baseout 0.001
        Rn0_0_1x0_res v2_0 n0_0_1x0_baseout 0.001
        Rn0_0_1x1_res v2_1 n0_0_1x1_baseout 0.001
        Rn0_0_1x2_res v2_2 n0_0_1x2_baseout 0.001
        
        Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
        Xn0_0x1_and n0_0_0x1_baseout n0_0_1x1_baseout n0_0x1_andout AND2
        Xn0_0x2_and n0_0_0x2_baseout n0_0_1x2_baseout n0_0x2_andout AND2

        Rn0x0_connect n0_0x0_andout v3_0 0.001
        Rn0x1_connect n0_0x1_andout v3_1 0.001
        Rn0x2_connect n0_0x2_andout v3_2 0.001
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check simulation rule and its output values
        SubcircuitReference subcircuitRef = new(module1, []);
        SignalReference v1Ref = new(subcircuitRef, v1);
        SignalReference v2Ref = new(subcircuitRef, v2);
        SignalReference v3Ref = new(subcircuitRef, v3);
        SimulationRule simRule = behavior.GetSimulationRule(v3Ref);
        Assert.AreEqual(v3Ref, simRule.OutputSignal);
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        for (int v1Val = 0; v1Val < 8; v1Val++)
            for (int v2Val = 0; v2Val < 8; v2Val++)
            {
                RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint(new()
                {
                    {v1Ref, [v1Val]},
                    {v2Ref, [v2Val]},
                }, [0, 1], 1);
                int value = simRule.OutputValueCalculation(state);
                int expectedValue = v1Val & v2Val;
                Assert.AreEqual(expectedValue, value);
            }
    }

    [TestMethod]
    public void LiteralTest()
    {
        Module m1 = new("m1");
        Vector input = m1.GenerateVector("input", 3);
        Vector output = m1.GenerateVector("output", 3);
        output.AssignBehavior(input.And(new Literal(5, 3)));

        string vhdl = output.Behavior!.GetVhdlStatement(output);
        string expectedVhdl = "output <= (input and \"101\");";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));

        string spice = output.Behavior!.GetSpice(output, "0").AsString();
        string expectedSpice = 
        $"""
        {Util.GetAndSubcircuitSpice(2, false)}
        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        
        Rn0_0_0x0_res input_0 n0_0_0x0_baseout 0.001
        Rn0_0_0x1_res input_1 n0_0_0x1_baseout 0.001
        Rn0_0_0x2_res input_2 n0_0_0x2_baseout 0.001
        
        Rn0_0_1x0_res VDD n0_0_1x0_baseout 0.001
        Rn0_0_1x1_res 0 n0_0_1x1_baseout 0.001
        Rn0_0_1x2_res VDD n0_0_1x2_baseout 0.001
        
        Xn0_0x0_and n0_0_0x0_baseout n0_0_1x0_baseout n0_0x0_andout AND2
        Xn0_0x1_and n0_0_0x1_baseout n0_0_1x1_baseout n0_0x1_andout AND2
        Xn0_0x2_and n0_0_0x2_baseout n0_0_1x2_baseout n0_0x2_andout AND2

        Rn0x0_connect n0_0x0_andout output_0 0.001
        Rn0x1_connect n0_0x1_andout output_1 0.001
        Rn0x2_connect n0_0x2_andout output_2 0.001
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check simulation rule and its output values
        SubcircuitReference subcircuitRef = new(m1, []);
        SignalReference inputRef = new(subcircuitRef, input);
        SignalReference outputRef = new(subcircuitRef, output);
        SimulationRule simRule = output.Behavior!.GetSimulationRule(outputRef);
        Assert.AreEqual(outputRef, simRule.OutputSignal);
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        for (int inputVal = 0; inputVal < 8; inputVal++)
        {
            RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint(new()
            {
                {inputRef, [inputVal]},
            }, [0, 1], 1);
            int value = simRule.OutputValueCalculation(state);
            int expectedValue = inputVal & 5;
            Assert.AreEqual(expectedValue, value);
        }
    }
}