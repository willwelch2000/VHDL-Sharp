using SpiceSharp.Components;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharpTests;

[TestClass]
public class StimulusTests
{
    [TestMethod]
    public void ConstantStimulusTest()
    {
        Module m1 = new();
        Signal s1 = m1.GenerateSignal("s1");
        ConstantStimulus stimulus = new(true);
        SpiceCircuit circuit = stimulus.GetSpice(s1, "0");
        Assert.AreEqual(1, circuit.CircuitElements.Count);
        VoltageSource source = circuit.CircuitElements.First() as VoltageSource ?? throw new();
        string[] nodes = [.. source.Nodes];
        Assert.AreEqual(2, nodes.Length);
        Assert.AreEqual("s1", nodes[0]);
        Assert.AreEqual("0", nodes[1]);
        Assert.AreEqual(5, source.Parameters.DcValue);

        // Sim rule
        SignalReference s1Ref = new(new(m1, []), s1);
        SimulationRule rule = stimulus.GetSimulationRule(s1Ref);
        Assert.AreEqual(s1Ref, rule.OutputSignal);
        Assert.AreEqual(0, rule.IndependentEventTimeGenerator(1).Count());
        RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint([], [0, 1], 1);
        Assert.AreEqual(1, rule.OutputValueCalculation(state));
    }
    
    [TestMethod]
    public void PulseStimulusTest()
    {
        Module m1 = new();
        Signal s1 = m1.GenerateSignal("s1");
        PulseStimulus stimulus = new(1, 2, 3);
        SpiceCircuit circuit = stimulus.GetSpice(s1, "0");
        Assert.AreEqual(1, circuit.CircuitElements.Count);
        VoltageSource source = circuit.CircuitElements.First() as VoltageSource ?? throw new();
        string[] nodes = [.. source.Nodes];
        Assert.AreEqual(2, nodes.Length);
        Assert.AreEqual("s1", nodes[0]);
        Assert.AreEqual("0", nodes[1]);
        Pulse pulse = source.Parameters.Waveform as Pulse ?? throw new();
        Assert.AreEqual(1, pulse.Delay);
        Assert.AreEqual(2, pulse.PulseWidth);
        Assert.AreEqual(3, pulse.Period);
        Assert.AreEqual(0, pulse.InitialValue);
        Assert.AreEqual(5, pulse.PulsedValue);

        // Sim rule
        SignalReference s1Ref = new(new(m1, []), s1);
        SimulationRule rule = stimulus.GetSimulationRule(s1Ref);
        Assert.AreEqual(s1Ref, rule.OutputSignal);
        Assert.AreEqual(3, rule.IndependentEventTimeGenerator(5).Count());
        RuleBasedSimulationState state1 = RuleBasedSimulationState.GivenStartingPoint([], [0, 0.9], 0.9);
        Assert.AreEqual(0, rule.OutputValueCalculation(state1));
        RuleBasedSimulationState state2 = RuleBasedSimulationState.GivenStartingPoint([], [0, 1.1], 1.1);
        Assert.AreEqual(1, rule.OutputValueCalculation(state2));
        RuleBasedSimulationState state3 = RuleBasedSimulationState.GivenStartingPoint([], [0, 2.9], 2.9);
        Assert.AreEqual(1, rule.OutputValueCalculation(state3));
        RuleBasedSimulationState state4 = RuleBasedSimulationState.GivenStartingPoint([], [0, 3.1], 3.1);
        Assert.AreEqual(0, rule.OutputValueCalculation(state4));
        RuleBasedSimulationState state5 = RuleBasedSimulationState.GivenStartingPoint([], [0, 3.9], 3.9);
        Assert.AreEqual(0, rule.OutputValueCalculation(state5));
        RuleBasedSimulationState state6 = RuleBasedSimulationState.GivenStartingPoint([], [0, 4.1], 4.1);
        Assert.AreEqual(1, rule.OutputValueCalculation(state6));
    }
    
    [TestMethod]
    public void TimeDefinedStimulusTest()
    {
        Module m1 = new();
        Signal s1 = m1.GenerateSignal("s1");
        TimeDefinedStimulus stimulus = new();
        stimulus.Points.Add(1, true);
        stimulus.Points.Add(2, false);
        SpiceCircuit circuit = stimulus.GetSpice(s1, "0");
        Assert.AreEqual(1, circuit.CircuitElements.Count);
        VoltageSource source = circuit.CircuitElements.First() as VoltageSource ?? throw new();
        string[] nodes = [.. source.Nodes];
        Assert.AreEqual(2, nodes.Length);
        Assert.AreEqual("s1", nodes[0]);
        Assert.AreEqual("0", nodes[1]);
        Pwl pwl = source.Parameters.Waveform as Pwl ?? throw new();
        Point[] points = [.. pwl.Points];
        Assert.AreEqual(4, points.Length);
        Assert.AreEqual(1, points[0].Time);
        Assert.AreEqual(5, points[0].Value);
        Assert.AreEqual(1.00000001, points[1].Time);
        Assert.AreEqual(5, points[1].Value);
        Assert.AreEqual(2, points[2].Time);
        Assert.AreEqual(5, points[2].Value);
        Assert.AreEqual(2.00000001, points[3].Time);
        Assert.AreEqual(0, points[3].Value);

        // Sim rule
        SignalReference s1Ref = new(new(m1, []), s1);
        SimulationRule rule = stimulus.GetSimulationRule(s1Ref);
        Assert.AreEqual(s1Ref, rule.OutputSignal);
        double[] times = [.. rule.IndependentEventTimeGenerator(5)];
        Assert.AreEqual(1, rule.IndependentEventTimeGenerator(5).Count());
        RuleBasedSimulationState state1 = RuleBasedSimulationState.GivenStartingPoint([], [0, 0.9], 0.9);
        Assert.AreEqual(1, rule.OutputValueCalculation(state1));
        RuleBasedSimulationState state2 = RuleBasedSimulationState.GivenStartingPoint([], [0, 1.1], 1.1);
        Assert.AreEqual(1, rule.OutputValueCalculation(state2));
        RuleBasedSimulationState state3 = RuleBasedSimulationState.GivenStartingPoint([], [0, 1.9], 1.9);
        Assert.AreEqual(1, rule.OutputValueCalculation(state3));
        RuleBasedSimulationState state4 = RuleBasedSimulationState.GivenStartingPoint([], [0, 2.1], 2.1);
        Assert.AreEqual(0, rule.OutputValueCalculation(state4));
        RuleBasedSimulationState state5 = RuleBasedSimulationState.GivenStartingPoint([], [0, 3], 3);
        Assert.AreEqual(0, rule.OutputValueCalculation(state5));
    }

    [TestMethod]
    public void MultiDimensionalConstantStimulusTest()
    {
        Module m1 = new();
        Vector v1 = m1.GenerateVector("v1", 3);
        MultiDimensionalConstantStimulus stimulus = new(6, 3);
        Assert.AreEqual(3, stimulus.Dimension.NonNullValue);
        Assert.AreEqual(6, stimulus.Value);
        SpiceCircuit circuit = stimulus.GetSpice(v1, "0");
        // Should all be voltage sources
        VoltageSource[] sources = [.. circuit.CircuitElements.Select(e => e as VoltageSource ?? throw new())];
        VoltageSource source0 = sources.First(s => s.Nodes[0] == "v1_0");
        Assert.AreEqual("0", source0.Nodes[1]);
        Assert.AreEqual(0, source0.Parameters.DcValue);
        VoltageSource source1 = sources.First(s => s.Nodes[0] == "v1_1");
        Assert.AreEqual("0", source1.Nodes[1]);
        Assert.AreEqual(5, source1.Parameters.DcValue);
        VoltageSource source2 = sources.First(s => s.Nodes[0] == "v1_2");
        Assert.AreEqual("0", source2.Nodes[1]);
        Assert.AreEqual(5, source2.Parameters.DcValue);

        // Sim rule
        SubmoduleReference m1Ref = new(m1, []);
        SignalReference v1Ref = m1Ref.GetChildSignalReference(v1);
        SimulationRule rule = stimulus.GetSimulationRule(v1Ref);
        Assert.AreEqual(rule.OutputSignal, v1Ref);
        Assert.AreEqual(0, rule.IndependentEventTimeGenerator(1).Count());
        RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint([], [0, 1], 1);
        Assert.AreEqual(6, rule.OutputValueCalculation(state));
    }

    [TestMethod]
    public void MultiDimensionalPulseStimulusTest()
    {
        Module m1 = new();
        Vector v1 = m1.GenerateVector("v1", 3);
        MultiDimensionalStimulus stimulus = new();
        stimulus.Stimuli.Add(new PulseStimulus(1, 2, 3));
        stimulus.Stimuli.Add(new PulseStimulus(1.1, 2, 3));
        stimulus.Stimuli.Add(new PulseStimulus(1.2, 2, 3));
        Assert.AreEqual(3, stimulus.Dimension.NonNullValue);
        SpiceCircuit circuit = stimulus.GetSpice(v1, "0");
        Assert.AreEqual(3, circuit.CircuitElements.Count);
        // Should all be voltage sources
        VoltageSource[] sources = [.. circuit.CircuitElements.Select(e => e as VoltageSource ?? throw new())];
        VoltageSource source0 = sources.First(s => s.Nodes[0] == "v1_0");
        Assert.AreEqual("0", source0.Nodes[1]);
        VoltageSource source1 = sources.First(s => s.Nodes[0] == "v1_1");
        Assert.AreEqual("0", source1.Nodes[1]);
        VoltageSource source2 = sources.First(s => s.Nodes[0] == "v1_2");
        Assert.AreEqual("0", source2.Nodes[1]);
        Pulse pulse = source0.Parameters.Waveform as Pulse ?? throw new();
        Assert.AreEqual(1, pulse.Delay);
        Assert.AreEqual(2, pulse.PulseWidth);
        Assert.AreEqual(3, pulse.Period);
        Assert.AreEqual(0, pulse.InitialValue);
        Assert.AreEqual(5, pulse.PulsedValue);

        // Sim rule
        SignalReference v1Ref = new(new(m1, []), v1);
        SimulationRule rule = stimulus.GetSimulationRule(v1Ref);
        Assert.AreEqual(v1Ref, rule.OutputSignal);
        Assert.AreEqual(9, rule.IndependentEventTimeGenerator(5).Count());
        RuleBasedSimulationState state1 = RuleBasedSimulationState.GivenStartingPoint([], [0, 0.9], 0.9);
        Assert.AreEqual(0, rule.OutputValueCalculation(state1));
        RuleBasedSimulationState state2 = RuleBasedSimulationState.GivenStartingPoint([], [0, 1.05], 1.05);
        Assert.AreEqual(1, rule.OutputValueCalculation(state2));
        RuleBasedSimulationState state3 = RuleBasedSimulationState.GivenStartingPoint([], [0, 1.15], 1.15);
        Assert.AreEqual(3, rule.OutputValueCalculation(state3));
        RuleBasedSimulationState state4 = RuleBasedSimulationState.GivenStartingPoint([], [0, 1.25], 1.25);
        Assert.AreEqual(7, rule.OutputValueCalculation(state4));
        RuleBasedSimulationState state5 = RuleBasedSimulationState.GivenStartingPoint([], [0, 3.05], 3.05);
        Assert.AreEqual(6, rule.OutputValueCalculation(state5));
        RuleBasedSimulationState state6 = RuleBasedSimulationState.GivenStartingPoint([], [0, 3.15], 3.15);
        Assert.AreEqual(4, rule.OutputValueCalculation(state6));
        RuleBasedSimulationState state7 = RuleBasedSimulationState.GivenStartingPoint([], [0, 3.25], 3.25);
        Assert.AreEqual(0, rule.OutputValueCalculation(state7));
    }
}