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
        
    }

    public static void MainTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        PortMapping portMapping = new(module1, module2);
        Signal s1 = new("s1", module1);
        Signal s2 = new("s2", module1);
        Signal s3 = new("s3", module1);
        Port p3 = new()
        {
            Signal = s3, 
            Direction = PortDirection.Output,
        };
        LogicExpression expression1 = new(s2.Not().And(s1));
        LogicTree<ISignal> expression2 = new And<ISignal>(expression1, new Or<ISignal>(s1, s2));
        module1.AddNewPort(s1, PortDirection.Input);
        module1.AddNewPort(s2, PortDirection.Input);
        module1.Ports.Add(p3);
        s3.Behavior = new LogicBehavior(s1.And(s2));

        Console.WriteLine(module1.ToSpice());

        Circuit circuit = module1.ToSpiceSharpCircuit();
        IEntity[] entities = [.. circuit];
    }

    public static void SimulationTest()
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
        Port p3 = module1.AddNewPort(s3, PortDirection.Output);
        module1.SignalBehaviors[s3] = new LogicBehavior(new And<ISignal>(s1, s2));
        // module1.SignalBehaviors[s3] = new LogicBehavior(new Not<ISignal>(s1));

        SimulationSetup setup = new(module1)
        {
            Length = 1e-3,
            StepSize = 1e-4,
        };
        SubcircuitReference subcircuit = new(module1, []);
        SignalReference reference1 = new(subcircuit, s1);
        SignalReference reference2 = new(subcircuit, s2);
        SignalReference reference3 = new(subcircuit, s3);
        setup.SignalsToMonitor.Add(reference1);
        setup.SignalsToMonitor.Add(reference2);
        setup.SignalsToMonitor.Add(reference3);
        setup.AssignStimulus(p1, new PulseStimulus(0.51e-3, 0.5e-3, 1e-3));
        TimeDefinedStimulus s1Stimulus = new();
        s1Stimulus.Points[0.1e-3] = false;
        s1Stimulus.Points[0.51e-3] = true;
        setup.AssignStimulus(p1, s1Stimulus);
        setup.AssignStimulus(p2, new PulseStimulus(0.25e-3, 0.25e-3, 0.5e-3));
        Console.WriteLine(setup.ToSpice());
        SimulationResult[] results = [.. setup.Simulate()];

        // Plot data
        ScottPlot.Plot plot = new();
        plot.Add.Scatter(results[0].TimeSteps, results[0].Values, ScottPlot.Color.FromHex("#CCCCFF"));
        plot.Add.Scatter(results[1].TimeSteps, results[1].Values, ScottPlot.Colors.Red);
        plot.Add.Scatter(results[2].TimeSteps, results[2].Values, ScottPlot.Colors.LightBlue);
        plot.SavePng("results.png", 400, 300);
    }

    public static void SimulationTest2()
    {
        Module module1 = new()
        {
            Name = "m1",
        };
        Vector selector = new("selector", module1, 2);
        Vector output = new("out", module1, 3);
        Port pOut = module1.AddNewPort(output, PortDirection.Output);
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
        setup.SignalsToMonitor.Add(new(subcircuit, selector));
        SimulationResult[] results = [.. setup.Simulate()];
        SimulationResult outputResults = results[0];
        SimulationResult selectorResults = results[1];
        ScottPlot.Plot plot = new();
        // plot.Add.Scatter(selectorResults.TimeSteps, selectorResults.Values, ScottPlot.Colors.Blue);
        plot.Add.Scatter(outputResults.TimeSteps, outputResults.Values, ScottPlot.Colors.Red);
        // plot.Add.Scatter(results[2].TimeSteps, results[2].Values, ScottPlot.Colors.LightBlue);
        plot.SavePng("results.png", 400, 300);
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
        EntityCollection entities = [];
        
        double vdd = 5;
        string nmosModelName = "NmosMod";
        string pmosModelName = "PmosMod";
        entities.Add(new VoltageSource("V_VDD", "VDD", "0", vdd));
        Mosfet1Model nmosModel = new(nmosModelName);
        nmosModel.Parameters.SetNmos(true);
        Mosfet1Model pmosModel = new(pmosModelName);
        pmosModel.Parameters.SetPmos(true);
        entities.Add(nmosModel);
        entities.Add(pmosModel);
        entities.Add(new Resistor("Rn0_0x0_res", "s1", "n0_0x0_baseout", 1e-3));
        entities.Add(new Resistor("Rn0_1x0_res", "s2", "n0_1x0_baseout", 1e-3));
        entities.Add(new Mosfet1("Mn0x0_pnand0", "n0x0_nandout", "n0_0x0_baseout", "VDD", "VDD", pmosModelName));
        entities.Add(new Mosfet1("Mn0x0_nnand0", "n0x0_nandout", "n0_0x0_baseout", "n0x0_nand1", "n0x0_nand1", nmosModelName));
        entities.Add(new Mosfet1("Mn0x0_pnand1", "n0x0_nandout", "n0_1x0_baseout", "VDD", "VDD", pmosModelName));
        entities.Add(new Mosfet1("Mn0x0_nnand1", "n0x0_nand1", "n0_1x0_baseout", "0", "0", nmosModelName));
        entities.Add(new VoltageSource("Vn0x0_const", "s1", "0", 0));
        entities.Add(new VoltageSource("Vn1x0_const", "s2", "0", 5));
        entities.Add(new Resistor("Rn0x0_floating", "n0x0_nandout", "0", 1e9));

        SubcircuitDefinition subcircuit = new(entities, "1", "2");

        Circuit circuit = [.. entities];
        
        var tran = new Transient("Tran 1", 1e-3, 0.1);

        var s1 = new RealVoltageExport(tran, "s1");
        var s2 = new RealVoltageExport(tran, "s2");
        var baseout1 = new RealVoltageExport(tran, "n0_0x0_baseout");
        var baseout2 = new RealVoltageExport(tran, "n0_1x0_baseout");

        int i = 0;
        foreach (int _ in tran.Run(circuit, Transient.ExportTransient))
        {
            Console.WriteLine($"i: {++i}");
            Console.WriteLine($"S1: {s1.Value}");
            Console.WriteLine($"S2: {s2.Value}");
            Console.WriteLine($"Baseout1: {baseout1.Value}");
            Console.WriteLine($"Baseout2: {baseout2.Value}");
        }
    }
}