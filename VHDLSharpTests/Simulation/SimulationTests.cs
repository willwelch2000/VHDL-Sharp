
using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class SimulationTests
{
    [TestMethod]
    public void AndExpressionSimulationTest()
    {
        Module module1 = new()
        {
            Name = "m1",
        };
        Signal s1 = new("s1", module1);
        Signal s2 = new("s2", module1);
        Signal s3 = new("s3", module1);
        Port p1 = module1.AddNewPort(s1, PortDirection.Input);
        Port p2 = module1.AddNewPort(s2, PortDirection.Input);
        module1.AddNewPort(s3, PortDirection.Output);
        module1.SignalBehaviors[s3] = new LogicBehavior(s1.And(s2));
        
        SimulationSetup setup = new(module1)
        {
            Length = 1e-3,
            StepSize = 1e-4,
        };
        SubcircuitReference subcircuit = new(module1, []);
        setup.SignalsToMonitor.Add(new(subcircuit, s1));
        setup.SignalsToMonitor.Add(new(subcircuit, s2));
        setup.SignalsToMonitor.Add(new(subcircuit, s3));
        
        TimeDefinedStimulus s1Stimulus = new();
        s1Stimulus.Points[0.1e-3] = false;
        s1Stimulus.Points[0.51e-3] = true;
        setup.AssignStimulus(p1, s1Stimulus);
        setup.AssignStimulus(p2, new PulseStimulus(0.25e-3, 0.25e-3, 0.5e-3));
        SimulationResult[] results = [.. setup.Simulate()];

        // Assert that all results of s3 match s1 AND s2
        // Assumes that time values are the same for all result sets
        if (!(results[0].TimeSteps.SequenceEqual(results[1].TimeSteps) && results[0].TimeSteps.SequenceEqual(results[2].TimeSteps)))
            return;
        int[] s1Values = results[0].Values;
        int[] s2Values = results[1].Values;
        int[] s3Values = results[2].Values;
        for (int i = 0; i < s1Values.Length; i++)
            Assert.AreEqual(s1Values[i] * s2Values[i], s3Values[i]); // ANDing = multiplication
    }

    [TestMethod]
    public void ValueBehaviorTest()
    {
        Module module1 = new()
        {
            Name = "m1",
        };
        Signal s1 = new("s1", module1);
        Signal s2 = new("s2", module1);
        Vector s3 = new("s3", module1, 4);
        Port p1 = module1.AddNewPort(s1, PortDirection.Input);
        module1.AddNewPort(s2, PortDirection.Output);
        module1.AddNewPort(s3, PortDirection.Output);
        module1.SignalBehaviors[s2] = new ValueBehavior(1);
        module1.SignalBehaviors[s3] = new ValueBehavior(10);
        
        SimulationSetup setup = new(module1)
        {
            Length = 1e-3,
            StepSize = 1e-4,
        };
        SubcircuitReference subcircuit = new(module1, []);
        SingleNodeNamedSignal[] s3SingleNodes = [.. s3.ToSingleNodeSignals];
        setup.SignalsToMonitor.Add(new(subcircuit, s2));
        setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[0]));
        setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[1]));
        setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[2]));
        setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[3]));
        setup.SignalsToMonitor.Add(new(subcircuit, s3));
        
        setup.AssignStimulus(p1, new PulseStimulus(0.25e-3, 0.25e-3, 0.5e-3));
        SimulationResult[] results = [.. setup.Simulate()];

        // s2 results
        int[] s2Results = results[0].Values;

        // Results for each bit of s3
        int[] s3_0 = results[1].Values;
        int[] s3_1 = results[2].Values;
        int[] s3_2 = results[3].Values;
        int[] s3_3 = results[4].Values;

        // Results for s3 overall
        int[] s3Results = results[5].Values;

        for (int i = 0; i < s2Results.Length; i++)
        {
            // Test s2
            Assert.AreEqual(1, s2Results[i]);
            // Test s3 bits
            Assert.AreEqual(0, s3_0[i]);
            Assert.AreEqual(1, s3_1[i]);
            Assert.AreEqual(0, s3_2[i]);
            Assert.AreEqual(1, s3_3[i]);
            // Test s3 overall
            Assert.AreEqual(10, s3Results[i]);
        }
    }

    [TestMethod]
    public void CaseBehaviorTest()
    {
        Module module1 = new()
        {
            Name = "m1",
        };
        Vector selector = new("selector", module1, 2);
        Vector output = new("out", module1, 3);
        module1.AddNewPort(output, PortDirection.Output);
        Port pSelector = module1.AddNewPort(selector, PortDirection.Input);
        CaseBehavior behavior =  new(selector);
        behavior.AddCase(0, new Literal(7, 3));
        behavior.AddCase(1, new Literal(6, 3));
        behavior.AddCase(2, new Literal(3, 3));
        behavior.SetDefault(new Literal(1, 3));
        module1.SignalBehaviors[output] = behavior;
        
        SimulationSetup setup = new(module1)
        {
            Length = 4e-3,
            StepSize = 1e-4,
        };
        Stimulus[] selectorStimuli = [
            new PulseStimulus(1e-3, 1e-3, 2e-3),
            new PulseStimulus(2e-3, 2e-3, 4e-3),
        ];
        setup.AssignStimulus(pSelector, new MultiDimensionalStimulus(selectorStimuli));
        SubcircuitReference subcircuit = new(module1, []);
        setup.SignalsToMonitor.Add(new(subcircuit, output));
        SimulationResult[] results = [.. setup.Simulate()];
        SimulationResult outputResults = results[0];
        double[] timeSteps = outputResults.TimeSteps;
        int[] values = outputResults.Values;
        for (int i = 0; i < outputResults.TimeSteps.Length; i++)
        {
            if (timeSteps[i] < 0.0009)
                Assert.AreEqual(7, values[i]);
            else if (timeSteps[i] > 0.0011 && timeSteps[i] < 0.0019)
                Assert.AreEqual(6, values[i]);
            else if (timeSteps[i] > 0.0021 && timeSteps[i] < 0.0029)
                Assert.AreEqual(3, values[i]);
            else if (timeSteps[i] > 0.0031)
                Assert.AreEqual(1, values[i]);
        }
    }
}