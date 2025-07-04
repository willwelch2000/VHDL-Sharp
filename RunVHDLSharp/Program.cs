using VHDLSharp.Behaviors;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;
using SpiceSharp.Entities;
using VHDLSharp.Conditions;
using VHDLSharp.SpiceCircuits;
using ScottPlot;
using VHDLSharp.Utility;

namespace VHDLSharp;

public static class Program
{
    public static void Main(string[] args)
    {
        TestDynamicSpice();
    }

    public static void MainTest()
    {
        Module module1 = new("m1");
        Module module2 = new("m2");
        PortMapping portMapping = new(module1, module2);
        Signal s1 = new("s1", module1);
        Signal s2 = new("s2", module1);
        Signal s3 = new("s3", module1);
        Port p3 = new(s3, PortDirection.Output);
        LogicExpression expression1 = new(s2.Not().And(s1));
        LogicTree<ISignal> expression2 = new And<ISignal>(expression1, new Or<ISignal>(s1, s2));
        module1.AddNewPort(s1, PortDirection.Input);
        module1.AddNewPort(s2, PortDirection.Input);
        module1.Ports.Add(p3);
        s3.Behavior = new LogicBehavior(s1.And(s2));

        Console.WriteLine(module1.GetSpice());

        Circuit circuit = module1.GetSpice().AsCircuit();
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

        SpiceBasedSimulation setup = new(module1)
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
        Console.WriteLine(setup.GetSpice());
        ISimulationResult[] results = [.. setup.Simulate()];

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
        CaseBehavior behavior = new(selector);
        behavior.AddCase(0, new Literal(7, 3));
        behavior.AddCase(1, new Literal(6, 3));
        behavior.AddCase(2, new Literal(3, 3));
        behavior.SetDefault(new Literal(1, 3));
        module1.SignalBehaviors[output] = behavior;

        SpiceBasedSimulation setup = new(module1)
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
        ISimulationResult[] results = [.. setup.Simulate()];
        ISimulationResult outputResults = results[0];
        ISimulationResult selectorResults = results[1];
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

        Console.WriteLine(constStimulus.GetSpice(s1, "0"));
        Console.WriteLine(pulseStimulus.GetSpice(s1, "1"));
        Console.WriteLine(timeDefinedStimulus.GetSpice(s1, "2"));
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

    public static void TestSpiceSharp2()
    {
        Module module1 = new("m1");
        Signal s1 = module1.GenerateSignal("s1");
        Signal s2 = module1.GenerateSignal("s2");
        Signal s3 = module1.GenerateSignal("s3");
        Vector v1 = module1.GenerateVector("v1", 3);
        Vector v2 = module1.GenerateVector("v2", 3);

        Equality equalitySingle = new(s1, s2);
        Equality equalityVector = v1.EqualityWith(v2);
        RisingEdge risingEdge = new(s1);
        FallingEdge fallingEdge = s1.FallingEdge();

        TimeDefinedStimulus s1Stimulus = new()
        {
            Points = new() { { 0, false }, { 2, true }, { 3, false } }
        };
        TimeDefinedStimulus s2Stimulus = new()
        {
            Points = new() { { 0, false }, { 1, true } }
        };
        MultiDimensionalStimulus v1Stimulus = new([
            new PulseStimulus(2, 10, 20),
            new ConstantStimulus(false),
            new PulseStimulus(2, 10, 20),
        ]);
        MultiDimensionalStimulus v2Stimulus = new([
            new PulseStimulus(1, 10, 20),
            new ConstantStimulus(false),
            new PulseStimulus(1, 10, 20),
        ]);
        SpiceCircuit stimuliCircuit = SpiceCircuit.Combine([
            s1Stimulus.GetSpice(s1, "s1Stimulus"),
            s2Stimulus.GetSpice(s2, "s2Stimulus"),
            v1Stimulus.GetSpice(v1, "v1Stimulus"),
            v2Stimulus.GetSpice(v2, "v2Stimulus"),
        ]);

        Circuit equalitySingleCircuit = equalitySingle.GetSpice("test", s3).CombineWith([stimuliCircuit]).AsCircuit();
        Circuit equalityVectorCircuit = equalityVector.GetSpice("test", s3).CombineWith([stimuliCircuit]).AsCircuit();
        Circuit risingEdgeCircuit = risingEdge.GetSpice("test", s3).CombineWith([stimuliCircuit]).AsCircuit();
        Circuit fallingEdgeCircuit = fallingEdge.GetSpice("test", s3).CombineWith([stimuliCircuit]).AsCircuit();

        var tran = new Transient("Tran 1", 0.1, 5);
        var s1Exp = new RealVoltageExport(tran, "s1");
        var s2Exp = new RealVoltageExport(tran, "s2");
        var s3Exp = new RealVoltageExport(tran, "s3");

        List<double> time = [];
        List<double> s1Results = [];
        List<double> s2Results = [];
        List<double> s3Results = [];
        int i = 0;
        foreach (int _ in tran.Run(equalitySingleCircuit, Transient.ExportTransient))
        {
            Console.WriteLine(i++);
            Console.WriteLine($"{tran.Time}: {s1Exp.Value}");
            Console.WriteLine();
            time.Add(tran.Time);
            s1Results.Add(s1Exp.Value);
            s2Results.Add(s2Exp.Value);
            s3Results.Add(s3Exp.Value);
        }

        Plot plot = new();
        plot.Add.ScatterLine(time, s1Results, Colors.Blue);
        plot.Add.ScatterLine(time, s2Results, Colors.Red);
        plot.Add.ScatterLine(time, s3Results, Colors.Green);
        plot.SavePng("test.png", 1000, 1000);
    }

    public static void TestSpiceSharp3()
    {
        Module module1 = new("m1");
        Signal d = module1.GenerateSignal("D");
        Signal clk = module1.GenerateSignal("CLK");
        Signal la = module1.GenerateSignal("LA");
        Signal da = module1.GenerateSignal("DA");
        Signal q = module1.GenerateSignal("Q");

        // SpiceCircuit dffCircuit = new(SpiceUtil.GetDffWithAsyncLoad().Entities);
        SpiceCircuit dffCircuit = new([]);
        Stimulus dStim = new PulseStimulus(1e-7, 1e-7, 2e-7);
        Stimulus daStim = new ConstantStimulus(false);
        Stimulus clkStim = new PulseStimulus(0.5e-7, 0.5e-7, 1e-7);
        Stimulus laStim = new ConstantStimulus(false);

        SpiceCircuit circuit = SpiceCircuit.Combine([
            dStim.GetSpice(d, "dStim"),
            daStim.GetSpice(da, "daStim"),
            clkStim.GetSpice(clk, "clkStim"),
            laStim.GetSpice(la, "laStim"),
            dffCircuit,
        ]);

        var tran = new Transient("Tran 1", 0.1e-7, 5e-7);
        var dExp = new RealVoltageExport(tran, "D");
        var daExp = new RealVoltageExport(tran, "DA");
        var clkExp = new RealVoltageExport(tran, "CLK");
        var laExp = new RealVoltageExport(tran, "LA");
        var qExp = new RealVoltageExport(tran, "Q");

        List<double> time = [];
        List<double> dResults = [];
        List<double> daResults = [];
        List<double> clkResults = [];
        List<double> laResults = [];
        List<double> qResults = [];

        foreach (int _ in tran.Run(circuit.AsCircuit(), Transient.ExportTransient))
        {
            time.Add(tran.Time);
            dResults.Add(dExp.Value);
            daResults.Add(daExp.Value);
            clkResults.Add(clkExp.Value);
            laResults.Add(laExp.Value);
            qResults.Add(qExp.Value);
        }

        Plot plot = new();
        plot.Add.ScatterLine(time, dResults, Colors.Blue);
        plot.Add.ScatterLine(time, clkResults, Colors.Red);
        plot.Add.ScatterLine(time, qResults, Colors.Green);
        plot.SavePng("test.png", 1000, 1000);
    }

    public static void TestSpiceSharpLatch()
    {
        // Circuit circuit = SpiceUtil.GetSrLatchCircuit();
        // circuit.Add(new VoltageSource("V1", "Sb", "0", new Pulse(5, 0, 4e-6, 1e-9, 1e-9, 1e-6, 5e-6)));
        // circuit.Add(new VoltageSource("V2", "Rb", "0", 5));

        // var tran = new Transient("Tran 1", 0.1e-6, 10e-6, 0.1e-6);
        // var sbExp = new RealVoltageExport(tran, "Sb");
        // var rbExp = new RealVoltageExport(tran, "Rb");
        // var qExp = new RealVoltageExport(tran, "Q");

        // List<double> time = [];
        // List<double> sbResults = [];
        // List<double> rbResults = [];
        // List<double> qResults = [];

        // foreach (int _ in tran.Run(circuit, Transient.ExportTransient))
        // {
        //     time.Add(tran.Time);
        //     sbResults.Add(sbExp.Value);
        //     rbResults.Add(rbExp.Value);
        //     qResults.Add(qExp.Value);
        // }

        // Plot plot = new();
        // plot.Add.ScatterLine(time, sbResults, Colors.Blue);
        // plot.Add.ScatterLine(time, rbResults, Colors.Red);
        // plot.Add.ScatterLine(time, qResults, Colors.Green);
        // plot.SavePng("test.png", 1000, 1000);
    }

    private static void AndLiteralTest()
    {
        Module m1 = new("m1");
        Vector input = m1.GenerateVector("input", 3);
        Vector output = m1.GenerateVector("output", 3);
        Port inputPort = m1.AddNewPort(input, PortDirection.Input);
        m1.AddNewPort(output, PortDirection.Output);
        output.AssignBehavior(input.And(new Literal(5, 3)));

        SpiceBasedSimulation setup = new(m1)
        {
            Length = 1e-3,
            StepSize = 1e-5,
        };

        SubcircuitReference subcircuit = new(m1, []);
        setup.SignalsToMonitor.Add(new(subcircuit, input));
        setup.SignalsToMonitor.Add(new(subcircuit, output));

        PulseStimulus stimulus2 = new(0.5e-3, 0.5e-3, 1e-3);
        PulseStimulus stimulus1 = new(0.25e-3, 0.25e-3, 0.5e-3);
        PulseStimulus stimulus0 = new(0.125e-3, 0.125e-3, 0.25e-3);

        MultiDimensionalStimulus stimulus = new([stimulus0, stimulus1, stimulus2]);
        setup.AssignStimulus(inputPort, stimulus);
        ISimulationResult[] results = [.. setup.Simulate()];

        // Plot data
        ScottPlot.Plot plot = new();
        plot.Add.Scatter(results[0].TimeSteps, results[0].Values, ScottPlot.Color.FromHex("#CCCCFF"));
        plot.Add.Scatter(results[1].TimeSteps, results[1].Values, ScottPlot.Colors.Red);
        plot.SavePng("results.png", 400, 300);
    }

    private static void TestDynamicSpice()
    {
        Module flipFlopModule = new("FlipFlopMod");
        Signal load = flipFlopModule.GenerateSignal("LOAD");
        Signal outSig = flipFlopModule.GenerateSignal("OUT");
        flipFlopModule.AddNewPort(load, PortDirection.Input);
        Port pIn = flipFlopModule.AddNewPort("IN", PortDirection.Input);
        flipFlopModule.AddNewPort(outSig, PortDirection.Output);
        DynamicBehavior behavior = new();
        behavior.ConditionMappings.Add((load.RisingEdge(), new LogicBehavior(pIn.Signal)));
        outSig.AssignBehavior(behavior);
        File.WriteAllText("OutputSpice.txt", flipFlopModule.GetSpice().AsString());
    }
}