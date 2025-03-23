using SpiceSharp.Components;
using SpiceSharp.Entities;
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
        Assert.AreEqual(1.000001, points[1].Time);
        Assert.AreEqual(5, points[1].Value);
        Assert.AreEqual(2, points[2].Time);
        Assert.AreEqual(5, points[2].Value);
        Assert.AreEqual(2.000001, points[3].Time);
        Assert.AreEqual(0, points[3].Value);
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
        Assert.AreEqual("v1_0", sources[0].Nodes[0]);
        Assert.AreEqual("0", sources[0].Nodes[1]);
        Assert.AreEqual(0, sources[0].Parameters.DcValue);
        Assert.AreEqual("v1_1", sources[1].Nodes[0]);
        Assert.AreEqual("0", sources[1].Nodes[1]);
        Assert.AreEqual(5, sources[1].Parameters.DcValue);
        Assert.AreEqual("v1_2", sources[2].Nodes[0]);
        Assert.AreEqual("0", sources[2].Nodes[1]);
        Assert.AreEqual(5, sources[2].Parameters.DcValue);
    }
}