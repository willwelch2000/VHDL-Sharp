using VHDLSharp.Behaviors;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;
using SpiceSharp.Entities;

namespace VHDLSharp;

public static class Program
{
    public static void Main(string[] args)
    {
        TestSpiceSharp();
    }

    public static void MainTest()
    {
        Module module1 = new()
        {
            Name = "m1",
        };
        Module module2 = new()
        {
            Name = "m2",
        };
        PortMapping portMapping = new(module1, module2);
        Signal s1 = new("s1", module1);
        Signal s2 = new("s2", module1);
        Signal s3 = new("s3", module1);
        Port p3 = new()
        {
            Signal = s3, 
            Direction = PortDirection.Output,
        };
        LogicExpression expression1 = new(new And<ISignal>(s1, new Not<ISignal>(s2)));
        LogicTree<ISignal> expression2 = new And<ISignal>(expression1, new Or<ISignal>(s1, s2));
        module1.AddNewPort(s1, PortDirection.Input);
        module1.AddNewPort(s2, PortDirection.Input);
        module1.Ports.Add(p3);
        module1.SignalBehaviors[s3] = new LogicBehavior(expression2);
        // module1.SignalBehaviors[s3] = new LogicBehavior(s2);

        Console.WriteLine(module1.ToSpice());

        List<IEntity> entities = [.. expression1.GetSpiceSharpEntities(s1, "0")];

        SubcircuitDefinition subcircuit = module1.ToSpiceSharpSubcircuit();
    }

    public static void StimulusTest()
    {
        Module module1 = new()
        {
            Name = "m1",
        };
        Signal s1 = new("s1", module1);

        Stimulus constStimulus = new ConstantStimulus()
        {
            Value = true,
        };

        Stimulus pulseStimulus = new PulseStimulus()
        {
            DelayTime = 1e-6,
            PulseWidth = 1e-6,
            Period = 2e-6,
        };

        TimeDefinedStimulus timeDefinedStimulus = new();
        timeDefinedStimulus.Points[1e-6] = true;
        timeDefinedStimulus.Points[2e-6] = false;
        timeDefinedStimulus.Points[3e-6] = false;
        timeDefinedStimulus.Points[4e-6] = true;

        Console.WriteLine(constStimulus.ToSpice(s1, "0"));
        Console.WriteLine(pulseStimulus.ToSpice(s1, "1"));
        Console.WriteLine(timeDefinedStimulus.ToSpice(s1, "2"));
    }

    public static void TestSpiceSharp()
    {
        EntityCollection entities = [
            new VoltageSource("V1", "1", "0", new Pulse(0, 5, 0.01, 1e-3, 1e-3, 0.02, 0.04)),
            new Resistor("R1", "1", "2", 1.0e4),
            new Capacitor("C1", "2", "0", 1e-6)];

        SubcircuitDefinition subcircuit = new(entities, "1", "2");

        Circuit circuit = [
            new Subcircuit("Xinst", subcircuit, "in", "out")
        ];
        
        var tran = new Transient("Tran 1", 1e-3, 0.1);

        var inputExport = new RealVoltageExport(tran, "in");
        var outputExport = new RealVoltageExport(tran, "out");

        int i = 0;
        foreach (int _ in tran.Run(circuit, Transient.ExportTransient))
        {
            double input = inputExport.Value;
            double output = outputExport.Value;

            Console.WriteLine($"i: {++i}");
            Console.WriteLine($"IN: {input}");
            Console.WriteLine($"OUT: {output}\n");
        }
    }
}