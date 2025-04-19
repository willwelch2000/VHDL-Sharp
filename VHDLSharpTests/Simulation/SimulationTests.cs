
using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class SimulationTests
{
    // Time buffer around transition points where we don't check
    private const double timeBuffer = 2e-6;

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
        
        Simulation[] setups = [new SpiceBasedSimulation(module1)
        {
            Length = 1e-3,
            StepSize = 1e-4,
        }, new RuleBasedSimulation(module1, new DefaultTimeStepGenerator {MaxTimeStep = 1e-4}) {Length = 1e-3}];
        foreach (Simulation setup in setups)
        {
            SubcircuitReference subcircuit = new(module1, []);
            setup.SignalsToMonitor.Add(new(subcircuit, s1));
            setup.SignalsToMonitor.Add(new(subcircuit, s2));
            setup.SignalsToMonitor.Add(new(subcircuit, s3));
            
            TimeDefinedStimulus s1Stimulus = new();
            s1Stimulus.Points[0.1e-3] = false;
            s1Stimulus.Points[0.51e-3] = true;
            setup.AssignStimulus(p1, s1Stimulus);
            setup.AssignStimulus(p2, new PulseStimulus(0.25e-3, 0.25e-3, 0.5e-3));
            ISimulationResult[] results = [.. setup.Simulate()];

            // Assert that all results of s3 match s1 AND s2
            // Assumes that time values are the same for all result sets
            if (!(results[0].TimeSteps.SequenceEqual(results[1].TimeSteps) && results[0].TimeSteps.SequenceEqual(results[2].TimeSteps)))
                throw new Exception("Expected timesteps to be the same for all results");
            int[] s1Values = results[0].Values;
            int[] s2Values = results[1].Values;
            int[] s3Values = results[2].Values;
            double[] timeSteps = results[0].TimeSteps;
            double[] transitionPoints = [0, 0.25e-3, 0.5e-3, 0.75e-3, 1e-3];
            for (int i = 0; i < s1Values.Length; i++)
            {
                // Skip points around time buffer
                if (transitionPoints.Select(p => Math.Abs(timeSteps[i] - p)).Min() < timeBuffer)
                    continue;
                Assert.AreEqual(s1Values[i] * s2Values[i], s3Values[i]); // ANDing = multiplication
            }
        }
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
        
        Simulation[] setups = [new SpiceBasedSimulation(module1)
        {
            Length = 1e-3,
            StepSize = 1e-4,
        }, new RuleBasedSimulation(module1, new DefaultTimeStepGenerator {MaxTimeStep = 1e-4}) {Length = 1e-3}];
        foreach (Simulation setup in setups)
        {
            SubcircuitReference subcircuit = new(module1, []);
            SingleNodeNamedSignal[] s3SingleNodes = [.. s3.ToSingleNodeSignals];
            setup.SignalsToMonitor.Add(new(subcircuit, s2));
            setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[0]));
            setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[1]));
            setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[2]));
            setup.SignalsToMonitor.Add(new(subcircuit, s3SingleNodes[3]));
            setup.SignalsToMonitor.Add(new(subcircuit, s3));
            
            setup.AssignStimulus(p1, new PulseStimulus(0.25e-3, 0.25e-3, 0.5e-3));
            ISimulationResult[] results = [.. setup.Simulate()];

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
        
        Simulation[] setups = [new SpiceBasedSimulation(module1)
        {
            Length = 4e-3,
            StepSize = 1e-4,
        }, new RuleBasedSimulation(module1, new DefaultTimeStepGenerator {MaxTimeStep = 1e-4}) {Length = 4e-3}];
        foreach (Simulation setup in setups)
        {
            Stimulus[] selectorStimuli = [
                new PulseStimulus(1e-3, 1e-3, 2e-3),
                new PulseStimulus(2e-3, 2e-3, 4e-3),
            ];
            setup.AssignStimulus(pSelector, new MultiDimensionalStimulus(selectorStimuli));
            SubcircuitReference subcircuit = new(module1, []);
            setup.SignalsToMonitor.Add(new(subcircuit, output));
            ISimulationResult[] results = [.. setup.Simulate()];
            ISimulationResult outputResults = results[0];
            double[] timeSteps = outputResults.TimeSteps;
            int[] values = outputResults.Values;
            for (int i = 0; i < outputResults.TimeSteps.Length; i++)
            {
                int? expectedResult = timeSteps[i] switch
                {
                    >      timeBuffer and < 1e-3-timeBuffer => 7,
                    > 1e-3+timeBuffer and < 2e-3-timeBuffer => 6,
                    > 2e-3+timeBuffer and < 3e-3-timeBuffer => 3,
                    > 3e-3+timeBuffer and < 4e-3-timeBuffer => 1,
                    _ => null,
                };

                if (expectedResult is not null)
                    Assert.AreEqual(expectedResult, values[i]);
            }
        }
    }

    [TestMethod]
    public void AndLiteralTest()
    {
        Module m1 = new("m1");
        Vector input = m1.GenerateVector("input", 3);
        Vector output = m1.GenerateVector("output", 3);
        Port inputPort = m1.AddNewPort(input, PortDirection.Input);
        m1.AddNewPort(output, PortDirection.Output);
        output.AssignBehavior(input.And(new Literal(5, 3)));

        Simulation[] setups = [new SpiceBasedSimulation(m1)
        {
            Length = 1e-3,
            StepSize = 1e-5,
        }, new RuleBasedSimulation(m1, new DefaultTimeStepGenerator {MaxTimeStep = 1e-5}) {Length = 1e-3}];
        foreach (Simulation setup in setups)
        {
            SubcircuitReference subcircuit = new(m1, []);
            setup.SignalsToMonitor.Add(new(subcircuit, input));
            setup.SignalsToMonitor.Add(new(subcircuit, output));

            PulseStimulus stimulus2 = new(0.5e-3, 0.5e-3, 1e-3);
            PulseStimulus stimulus1 = new(0.25e-3, 0.25e-3, 0.5e-3);
            PulseStimulus stimulus0 = new(0.125e-3, 0.125e-3, 0.25e-3);

            MultiDimensionalStimulus stimulus = new([stimulus0, stimulus1, stimulus2]);
            setup.AssignStimulus(inputPort, stimulus);
            ISimulationResult[] results = [.. setup.Simulate()];

            ISimulationResult outputResult = results[1];
            for (int i = 0; i < outputResult.TimeSteps.Length; i++)
            {
                double time = outputResult.TimeSteps[i];
                int? expectedResult = time switch
                {
                    >          timeBuffer and < 0.125e-3-timeBuffer => 0,
                    > 0.125e-3+timeBuffer and < 0.250e-3-timeBuffer => 1,
                    > 0.250e-3+timeBuffer and < 0.375e-3-timeBuffer => 0,
                    > 0.375e-3+timeBuffer and < 0.500e-3-timeBuffer => 1,
                    > 0.500e-3+timeBuffer and < 0.625e-3-timeBuffer => 4,
                    > 0.625e-3+timeBuffer and < 0.750e-3-timeBuffer => 5,
                    > 0.750e-3+timeBuffer and < 0.875e-3-timeBuffer => 4,
                    > 0.875e-3+timeBuffer and < 1.000e-3-timeBuffer => 5,
                    _ => null,
                };

                if (expectedResult is not null)
                    Assert.AreEqual(expectedResult, outputResult.Values[i]);
            }
        }
    }
}