using VHDLSharp.Behaviors;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;
using SpiceSharp.Entities;
using VHDLSharp.Validation;

namespace VHDLSharp;

public static class Program
{
    public static void Main(string[] args)
    {
        CompareStrings();
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
        Console.WriteLine(setup.GetSpice());
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

    private static void CompareStrings()
    {
        string s1 = 
        """
        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        Rn0_0_0x0_res VDD n0_0_0x0_baseout 0.001
        Rn0_0_0x1_res VDD n0_0_0x1_baseout 0.001
        Rn0_0_0x2_res VDD n0_0_0x2_baseout 0.001
        Rn0_0x0_connect n0_0_0x0_baseout n0x0_case0_0 0.001
        Rn0_0x1_connect n0_0_0x1_baseout n0x0_case0_1 0.001
        Rn0_0x2_connect n0_0_0x2_baseout n0x0_case0_2 0.001
        Rn0_1_0x0_res 0 n0_1_0x0_baseout 0.001
        Rn0_1_0x1_res VDD n0_1_0x1_baseout 0.001
        Rn0_1_0x2_res VDD n0_1_0x2_baseout 0.001
        Rn0_1x0_connect n0_1_0x0_baseout n0x0_case1_0 0.001
        Rn0_1x1_connect n0_1_0x1_baseout n0x0_case1_1 0.001
        Rn0_1x2_connect n0_1_0x2_baseout n0x0_case1_2 0.001
        Rn0_2_0x0_res VDD n0_2_0x0_baseout 0.001
        Rn0_2_0x1_res VDD n0_2_0x1_baseout 0.001
        Rn0_2_0x2_res 0 n0_2_0x2_baseout 0.001
        Rn0_2x0_connect n0_2_0x0_baseout n0x0_case2_0 0.001
        Rn0_2x1_connect n0_2_0x1_baseout n0x0_case2_1 0.001
        Rn0_2x2_connect n0_2_0x2_baseout n0x0_case2_2 0.001
        Rn0_3_0x0_res VDD n0_3_0x0_baseout 0.001
        Rn0_3_0x1_res 0 n0_3_0x1_baseout 0.001
        Rn0_3_0x2_res 0 n0_3_0x2_baseout 0.001
        Rn0_3x0_connect n0_3_0x0_baseout n0x0_case3_0 0.001
        Rn0_3x1_connect n0_3_0x1_baseout n0x0_case3_1 0.001
        Rn0_3x2_connect n0_3_0x2_baseout n0x0_case3_2 0.001
        Rn0_4_0_0_0_0x0_res selector_0 n0_4_0_0_0_0x0_baseout 0.001
        Mn0_4_0_0_0x0_p n0_4_0_0_0x0_notout n0_4_0_0_0_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_0_0x0_n n0_4_0_0_0x0_notout n0_4_0_0_0_0x0_baseout 0 0 NmosMod
        Rn0_4_0_0_1_0x0_res selector_1 n0_4_0_0_1_0x0_baseout 0.001
        Mn0_4_0_0_1x0_p n0_4_0_0_1x0_notout n0_4_0_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_0_1x0_n n0_4_0_0_1x0_notout n0_4_0_0_1_0x0_baseout 0 0 NmosMod
        Rn0_4_0_0_2x0_res n0x0_case0_0 n0_4_0_0_2x0_baseout 0.001
        Mn0_4_0_0x0_pnand0 n0_4_0_0x0_nandout n0_4_0_0_0x0_notout VDD VDD PmosMod
        Mn0_4_0_0x0_nnand0 n0_4_0_0x0_nandout n0_4_0_0_0x0_notout n0_4_0_0x0_nand1 n0_4_0_0x0_nand1 NmosMod
        Mn0_4_0_0x0_pnand1 n0_4_0_0x0_nandout n0_4_0_0_1x0_notout VDD VDD PmosMod
        Mn0_4_0_0x0_nnand1 n0_4_0_0x0_nand1 n0_4_0_0_1x0_notout n0_4_0_0x0_nand2 n0_4_0_0x0_nand2 NmosMod
        Mn0_4_0_0x0_pnand2 n0_4_0_0x0_nandout n0_4_0_0_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_0x0_nnand2 n0_4_0_0x0_nand2 n0_4_0_0_2x0_baseout 0 0 NmosMod
        Mn0_4_0_0x0_pnot n0_4_0_0x0_andout n0_4_0_0x0_nandout VDD VDD PmosMod
        Mn0_4_0_0x0_nnot n0_4_0_0x0_andout n0_4_0_0x0_nandout 0 0 NmosMod
        Rn0_4_0_1_0x0_res selector_0 n0_4_0_1_0x0_baseout 0.001
        Rn0_4_0_1_1_0x0_res selector_1 n0_4_0_1_1_0x0_baseout 0.001
        Mn0_4_0_1_1x0_p n0_4_0_1_1x0_notout n0_4_0_1_1_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_1_1x0_n n0_4_0_1_1x0_notout n0_4_0_1_1_0x0_baseout 0 0 NmosMod
        Rn0_4_0_1_2x0_res n0x0_case1_0 n0_4_0_1_2x0_baseout 0.001
        Mn0_4_0_1x0_pnand0 n0_4_0_1x0_nandout n0_4_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_1x0_nnand0 n0_4_0_1x0_nandout n0_4_0_1_0x0_baseout n0_4_0_1x0_nand1 n0_4_0_1x0_nand1 NmosMod
        Mn0_4_0_1x0_pnand1 n0_4_0_1x0_nandout n0_4_0_1_1x0_notout VDD VDD PmosMod
        Mn0_4_0_1x0_nnand1 n0_4_0_1x0_nand1 n0_4_0_1_1x0_notout n0_4_0_1x0_nand2 n0_4_0_1x0_nand2 NmosMod
        Mn0_4_0_1x0_pnand2 n0_4_0_1x0_nandout n0_4_0_1_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_1x0_nnand2 n0_4_0_1x0_nand2 n0_4_0_1_2x0_baseout 0 0 NmosMod
        Mn0_4_0_1x0_pnot n0_4_0_1x0_andout n0_4_0_1x0_nandout VDD VDD PmosMod
        Mn0_4_0_1x0_nnot n0_4_0_1x0_andout n0_4_0_1x0_nandout 0 0 NmosMod
        Rn0_4_0_2_0_0x0_res selector_0 n0_4_0_2_0_0x0_baseout 0.001
        Mn0_4_0_2_0x0_p n0_4_0_2_0x0_notout n0_4_0_2_0_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_2_0x0_n n0_4_0_2_0x0_notout n0_4_0_2_0_0x0_baseout 0 0 NmosMod
        Rn0_4_0_2_1x0_res selector_1 n0_4_0_2_1x0_baseout 0.001
        Rn0_4_0_2_2x0_res n0x0_case2_0 n0_4_0_2_2x0_baseout 0.001
        Mn0_4_0_2x0_pnand0 n0_4_0_2x0_nandout n0_4_0_2_0x0_notout VDD VDD PmosMod
        Mn0_4_0_2x0_nnand0 n0_4_0_2x0_nandout n0_4_0_2_0x0_notout n0_4_0_2x0_nand1 n0_4_0_2x0_nand1 NmosMod
        Mn0_4_0_2x0_pnand1 n0_4_0_2x0_nandout n0_4_0_2_1x0_baseout VDD VDD PmosMod
        Mn0_4_0_2x0_nnand1 n0_4_0_2x0_nand1 n0_4_0_2_1x0_baseout n0_4_0_2x0_nand2 n0_4_0_2x0_nand2 NmosMod
        Mn0_4_0_2x0_pnand2 n0_4_0_2x0_nandout n0_4_0_2_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_2x0_nnand2 n0_4_0_2x0_nand2 n0_4_0_2_2x0_baseout 0 0 NmosMod
        Mn0_4_0_2x0_pnot n0_4_0_2x0_andout n0_4_0_2x0_nandout VDD VDD PmosMod
        Mn0_4_0_2x0_nnot n0_4_0_2x0_andout n0_4_0_2x0_nandout 0 0 NmosMod
        Rn0_4_0_3_0x0_res selector_0 n0_4_0_3_0x0_baseout 0.001
        Rn0_4_0_3_1x0_res selector_1 n0_4_0_3_1x0_baseout 0.001
        Rn0_4_0_3_2x0_res n0x0_case3_0 n0_4_0_3_2x0_baseout 0.001
        Mn0_4_0_3x0_pnand0 n0_4_0_3x0_nandout n0_4_0_3_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_3x0_nnand0 n0_4_0_3x0_nandout n0_4_0_3_0x0_baseout n0_4_0_3x0_nand1 n0_4_0_3x0_nand1 NmosMod
        Mn0_4_0_3x0_pnand1 n0_4_0_3x0_nandout n0_4_0_3_1x0_baseout VDD VDD PmosMod
        Mn0_4_0_3x0_nnand1 n0_4_0_3x0_nand1 n0_4_0_3_1x0_baseout n0_4_0_3x0_nand2 n0_4_0_3x0_nand2 NmosMod
        Mn0_4_0_3x0_pnand2 n0_4_0_3x0_nandout n0_4_0_3_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_3x0_nnand2 n0_4_0_3x0_nand2 n0_4_0_3_2x0_baseout 0 0 NmosMod
        Mn0_4_0_3x0_pnot n0_4_0_3x0_andout n0_4_0_3x0_nandout VDD VDD PmosMod
        Mn0_4_0_3x0_nnot n0_4_0_3x0_andout n0_4_0_3x0_nandout 0 0 NmosMod
        Mn0_4_0x0_pnor0 n0_4_0x0_norout n0_4_0_0x0_andout n0_4_0x0_nor1 n0_4_0x0_nor1 PmosMod
        Mn0_4_0x0_nnor0 n0_4_0x0_norout n0_4_0_0x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnor1 n0_4_0x0_nor1 n0_4_0_1x0_andout n0_4_0x0_nor2 n0_4_0x0_nor2 PmosMod
        Mn0_4_0x0_nnor1 n0_4_0x0_norout n0_4_0_1x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnor2 n0_4_0x0_nor2 n0_4_0_2x0_andout n0_4_0x0_nor3 n0_4_0x0_nor3 PmosMod
        Mn0_4_0x0_nnor2 n0_4_0x0_norout n0_4_0_2x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnor3 n0_4_0x0_nor3 n0_4_0_3x0_andout VDD VDD PmosMod
        Mn0_4_0x0_nnor3 n0_4_0x0_norout n0_4_0_3x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnot n0_4_0x0_orout n0_4_0x0_norout VDD VDD PmosMod
        Mn0_4_0x0_nnot n0_4_0x0_orout n0_4_0x0_norout 0 0 NmosMod
        Rn0_4x0_connect n0_4_0x0_orout v1_0 0.001
        Rn0_5_0_0_0_0x0_res selector_0 n0_5_0_0_0_0x0_baseout 0.001
        Mn0_5_0_0_0x0_p n0_5_0_0_0x0_notout n0_5_0_0_0_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_0_0x0_n n0_5_0_0_0x0_notout n0_5_0_0_0_0x0_baseout 0 0 NmosMod
        Rn0_5_0_0_1_0x0_res selector_1 n0_5_0_0_1_0x0_baseout 0.001
        Mn0_5_0_0_1x0_p n0_5_0_0_1x0_notout n0_5_0_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_0_1x0_n n0_5_0_0_1x0_notout n0_5_0_0_1_0x0_baseout 0 0 NmosMod
        Rn0_5_0_0_2x0_res n0x0_case0_1 n0_5_0_0_2x0_baseout 0.001
        Mn0_5_0_0x0_pnand0 n0_5_0_0x0_nandout n0_5_0_0_0x0_notout VDD VDD PmosMod
        Mn0_5_0_0x0_nnand0 n0_5_0_0x0_nandout n0_5_0_0_0x0_notout n0_5_0_0x0_nand1 n0_5_0_0x0_nand1 NmosMod
        Mn0_5_0_0x0_pnand1 n0_5_0_0x0_nandout n0_5_0_0_1x0_notout VDD VDD PmosMod
        Mn0_5_0_0x0_nnand1 n0_5_0_0x0_nand1 n0_5_0_0_1x0_notout n0_5_0_0x0_nand2 n0_5_0_0x0_nand2 NmosMod
        Mn0_5_0_0x0_pnand2 n0_5_0_0x0_nandout n0_5_0_0_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_0x0_nnand2 n0_5_0_0x0_nand2 n0_5_0_0_2x0_baseout 0 0 NmosMod
        Mn0_5_0_0x0_pnot n0_5_0_0x0_andout n0_5_0_0x0_nandout VDD VDD PmosMod
        Mn0_5_0_0x0_nnot n0_5_0_0x0_andout n0_5_0_0x0_nandout 0 0 NmosMod
        Rn0_5_0_1_0x0_res selector_0 n0_5_0_1_0x0_baseout 0.001
        Rn0_5_0_1_1_0x0_res selector_1 n0_5_0_1_1_0x0_baseout 0.001
        Mn0_5_0_1_1x0_p n0_5_0_1_1x0_notout n0_5_0_1_1_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_1_1x0_n n0_5_0_1_1x0_notout n0_5_0_1_1_0x0_baseout 0 0 NmosMod
        Rn0_5_0_1_2x0_res n0x0_case1_1 n0_5_0_1_2x0_baseout 0.001
        Mn0_5_0_1x0_pnand0 n0_5_0_1x0_nandout n0_5_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_1x0_nnand0 n0_5_0_1x0_nandout n0_5_0_1_0x0_baseout n0_5_0_1x0_nand1 n0_5_0_1x0_nand1 NmosMod
        Mn0_5_0_1x0_pnand1 n0_5_0_1x0_nandout n0_5_0_1_1x0_notout VDD VDD PmosMod
        Mn0_5_0_1x0_nnand1 n0_5_0_1x0_nand1 n0_5_0_1_1x0_notout n0_5_0_1x0_nand2 n0_5_0_1x0_nand2 NmosMod
        Mn0_5_0_1x0_pnand2 n0_5_0_1x0_nandout n0_5_0_1_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_1x0_nnand2 n0_5_0_1x0_nand2 n0_5_0_1_2x0_baseout 0 0 NmosMod
        Mn0_5_0_1x0_pnot n0_5_0_1x0_andout n0_5_0_1x0_nandout VDD VDD PmosMod
        Mn0_5_0_1x0_nnot n0_5_0_1x0_andout n0_5_0_1x0_nandout 0 0 NmosMod
        Rn0_5_0_2_0_0x0_res selector_0 n0_5_0_2_0_0x0_baseout 0.001
        Mn0_5_0_2_0x0_p n0_5_0_2_0x0_notout n0_5_0_2_0_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_2_0x0_n n0_5_0_2_0x0_notout n0_5_0_2_0_0x0_baseout 0 0 NmosMod
        Rn0_5_0_2_1x0_res selector_1 n0_5_0_2_1x0_baseout 0.001
        Rn0_5_0_2_2x0_res n0x0_case2_1 n0_5_0_2_2x0_baseout 0.001
        Mn0_5_0_2x0_pnand0 n0_5_0_2x0_nandout n0_5_0_2_0x0_notout VDD VDD PmosMod
        Mn0_5_0_2x0_nnand0 n0_5_0_2x0_nandout n0_5_0_2_0x0_notout n0_5_0_2x0_nand1 n0_5_0_2x0_nand1 NmosMod
        Mn0_5_0_2x0_pnand1 n0_5_0_2x0_nandout n0_5_0_2_1x0_baseout VDD VDD PmosMod
        Mn0_5_0_2x0_nnand1 n0_5_0_2x0_nand1 n0_5_0_2_1x0_baseout n0_5_0_2x0_nand2 n0_5_0_2x0_nand2 NmosMod
        Mn0_5_0_2x0_pnand2 n0_5_0_2x0_nandout n0_5_0_2_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_2x0_nnand2 n0_5_0_2x0_nand2 n0_5_0_2_2x0_baseout 0 0 NmosMod
        Mn0_5_0_2x0_pnot n0_5_0_2x0_andout n0_5_0_2x0_nandout VDD VDD PmosMod
        Mn0_5_0_2x0_nnot n0_5_0_2x0_andout n0_5_0_2x0_nandout 0 0 NmosMod
        Rn0_5_0_3_0x0_res selector_0 n0_5_0_3_0x0_baseout 0.001
        Rn0_5_0_3_1x0_res selector_1 n0_5_0_3_1x0_baseout 0.001
        Rn0_5_0_3_2x0_res n0x0_case3_1 n0_5_0_3_2x0_baseout 0.001
        Mn0_5_0_3x0_pnand0 n0_5_0_3x0_nandout n0_5_0_3_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_3x0_nnand0 n0_5_0_3x0_nandout n0_5_0_3_0x0_baseout n0_5_0_3x0_nand1 n0_5_0_3x0_nand1 NmosMod
        Mn0_5_0_3x0_pnand1 n0_5_0_3x0_nandout n0_5_0_3_1x0_baseout VDD VDD PmosMod
        Mn0_5_0_3x0_nnand1 n0_5_0_3x0_nand1 n0_5_0_3_1x0_baseout n0_5_0_3x0_nand2 n0_5_0_3x0_nand2 NmosMod
        Mn0_5_0_3x0_pnand2 n0_5_0_3x0_nandout n0_5_0_3_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_3x0_nnand2 n0_5_0_3x0_nand2 n0_5_0_3_2x0_baseout 0 0 NmosMod
        Mn0_5_0_3x0_pnot n0_5_0_3x0_andout n0_5_0_3x0_nandout VDD VDD PmosMod
        Mn0_5_0_3x0_nnot n0_5_0_3x0_andout n0_5_0_3x0_nandout 0 0 NmosMod
        Mn0_5_0x0_pnor0 n0_5_0x0_norout n0_5_0_0x0_andout n0_5_0x0_nor1 n0_5_0x0_nor1 PmosMod
        Mn0_5_0x0_nnor0 n0_5_0x0_norout n0_5_0_0x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnor1 n0_5_0x0_nor1 n0_5_0_1x0_andout n0_5_0x0_nor2 n0_5_0x0_nor2 PmosMod
        Mn0_5_0x0_nnor1 n0_5_0x0_norout n0_5_0_1x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnor2 n0_5_0x0_nor2 n0_5_0_2x0_andout n0_5_0x0_nor3 n0_5_0x0_nor3 PmosMod
        Mn0_5_0x0_nnor2 n0_5_0x0_norout n0_5_0_2x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnor3 n0_5_0x0_nor3 n0_5_0_3x0_andout VDD VDD PmosMod
        Mn0_5_0x0_nnor3 n0_5_0x0_norout n0_5_0_3x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnot n0_5_0x0_orout n0_5_0x0_norout VDD VDD PmosMod
        Mn0_5_0x0_nnot n0_5_0x0_orout n0_5_0x0_norout 0 0 NmosMod
        Rn0_5x0_connect n0_5_0x0_orout v1_1 0.001
        Rn0_6_0_0_0_0x0_res selector_0 n0_6_0_0_0_0x0_baseout 0.001
        Mn0_6_0_0_0x0_p n0_6_0_0_0x0_notout n0_6_0_0_0_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_0_0x0_n n0_6_0_0_0x0_notout n0_6_0_0_0_0x0_baseout 0 0 NmosMod
        Rn0_6_0_0_1_0x0_res selector_1 n0_6_0_0_1_0x0_baseout 0.001
        Mn0_6_0_0_1x0_p n0_6_0_0_1x0_notout n0_6_0_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_0_1x0_n n0_6_0_0_1x0_notout n0_6_0_0_1_0x0_baseout 0 0 NmosMod
        Rn0_6_0_0_2x0_res n0x0_case0_2 n0_6_0_0_2x0_baseout 0.001
        Mn0_6_0_0x0_pnand0 n0_6_0_0x0_nandout n0_6_0_0_0x0_notout VDD VDD PmosMod
        Mn0_6_0_0x0_nnand0 n0_6_0_0x0_nandout n0_6_0_0_0x0_notout n0_6_0_0x0_nand1 n0_6_0_0x0_nand1 NmosMod
        Mn0_6_0_0x0_pnand1 n0_6_0_0x0_nandout n0_6_0_0_1x0_notout VDD VDD PmosMod
        Mn0_6_0_0x0_nnand1 n0_6_0_0x0_nand1 n0_6_0_0_1x0_notout n0_6_0_0x0_nand2 n0_6_0_0x0_nand2 NmosMod
        Mn0_6_0_0x0_pnand2 n0_6_0_0x0_nandout n0_6_0_0_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_0x0_nnand2 n0_6_0_0x0_nand2 n0_6_0_0_2x0_baseout 0 0 NmosMod
        Mn0_6_0_0x0_pnot n0_6_0_0x0_andout n0_6_0_0x0_nandout VDD VDD PmosMod
        Mn0_6_0_0x0_nnot n0_6_0_0x0_andout n0_6_0_0x0_nandout 0 0 NmosMod
        Rn0_6_0_1_0x0_res selector_0 n0_6_0_1_0x0_baseout 0.001
        Rn0_6_0_1_1_0x0_res selector_1 n0_6_0_1_1_0x0_baseout 0.001
        Mn0_6_0_1_1x0_p n0_6_0_1_1x0_notout n0_6_0_1_1_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_1_1x0_n n0_6_0_1_1x0_notout n0_6_0_1_1_0x0_baseout 0 0 NmosMod
        Rn0_6_0_1_2x0_res n0x0_case1_2 n0_6_0_1_2x0_baseout 0.001
        Mn0_6_0_1x0_pnand0 n0_6_0_1x0_nandout n0_6_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_1x0_nnand0 n0_6_0_1x0_nandout n0_6_0_1_0x0_baseout n0_6_0_1x0_nand1 n0_6_0_1x0_nand1 NmosMod
        Mn0_6_0_1x0_pnand1 n0_6_0_1x0_nandout n0_6_0_1_1x0_notout VDD VDD PmosMod
        Mn0_6_0_1x0_nnand1 n0_6_0_1x0_nand1 n0_6_0_1_1x0_notout n0_6_0_1x0_nand2 n0_6_0_1x0_nand2 NmosMod
        Mn0_6_0_1x0_pnand2 n0_6_0_1x0_nandout n0_6_0_1_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_1x0_nnand2 n0_6_0_1x0_nand2 n0_6_0_1_2x0_baseout 0 0 NmosMod
        Mn0_6_0_1x0_pnot n0_6_0_1x0_andout n0_6_0_1x0_nandout VDD VDD PmosMod
        Mn0_6_0_1x0_nnot n0_6_0_1x0_andout n0_6_0_1x0_nandout 0 0 NmosMod
        Rn0_6_0_2_0_0x0_res selector_0 n0_6_0_2_0_0x0_baseout 0.001
        Mn0_6_0_2_0x0_p n0_6_0_2_0x0_notout n0_6_0_2_0_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_2_0x0_n n0_6_0_2_0x0_notout n0_6_0_2_0_0x0_baseout 0 0 NmosMod
        Rn0_6_0_2_1x0_res selector_1 n0_6_0_2_1x0_baseout 0.001
        Rn0_6_0_2_2x0_res n0x0_case2_2 n0_6_0_2_2x0_baseout 0.001
        Mn0_6_0_2x0_pnand0 n0_6_0_2x0_nandout n0_6_0_2_0x0_notout VDD VDD PmosMod
        Mn0_6_0_2x0_nnand0 n0_6_0_2x0_nandout n0_6_0_2_0x0_notout n0_6_0_2x0_nand1 n0_6_0_2x0_nand1 NmosMod
        Mn0_6_0_2x0_pnand1 n0_6_0_2x0_nandout n0_6_0_2_1x0_baseout VDD VDD PmosMod
        Mn0_6_0_2x0_nnand1 n0_6_0_2x0_nand1 n0_6_0_2_1x0_baseout n0_6_0_2x0_nand2 n0_6_0_2x0_nand2 NmosMod
        Mn0_6_0_2x0_pnand2 n0_6_0_2x0_nandout n0_6_0_2_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_2x0_nnand2 n0_6_0_2x0_nand2 n0_6_0_2_2x0_baseout 0 0 NmosMod
        Mn0_6_0_2x0_pnot n0_6_0_2x0_andout n0_6_0_2x0_nandout VDD VDD PmosMod
        Mn0_6_0_2x0_nnot n0_6_0_2x0_andout n0_6_0_2x0_nandout 0 0 NmosMod
        Rn0_6_0_3_0x0_res selector_0 n0_6_0_3_0x0_baseout 0.001
        Rn0_6_0_3_1x0_res selector_1 n0_6_0_3_1x0_baseout 0.001
        Rn0_6_0_3_2x0_res n0x0_case3_2 n0_6_0_3_2x0_baseout 0.001
        Mn0_6_0_3x0_pnand0 n0_6_0_3x0_nandout n0_6_0_3_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_3x0_nnand0 n0_6_0_3x0_nandout n0_6_0_3_0x0_baseout n0_6_0_3x0_nand1 n0_6_0_3x0_nand1 NmosMod
        Mn0_6_0_3x0_pnand1 n0_6_0_3x0_nandout n0_6_0_3_1x0_baseout VDD VDD PmosMod
        Mn0_6_0_3x0_nnand1 n0_6_0_3x0_nand1 n0_6_0_3_1x0_baseout n0_6_0_3x0_nand2 n0_6_0_3x0_nand2 NmosMod
        Mn0_6_0_3x0_pnand2 n0_6_0_3x0_nandout n0_6_0_3_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_3x0_nnand2 n0_6_0_3x0_nand2 n0_6_0_3_2x0_baseout 0 0 NmosMod
        Mn0_6_0_3x0_pnot n0_6_0_3x0_andout n0_6_0_3x0_nandout VDD VDD PmosMod
        Mn0_6_0_3x0_nnot n0_6_0_3x0_andout n0_6_0_3x0_nandout 0 0 NmosMod
        Mn0_6_0x0_pnor0 n0_6_0x0_norout n0_6_0_0x0_andout n0_6_0x0_nor1 n0_6_0x0_nor1 PmosMod
        Mn0_6_0x0_nnor0 n0_6_0x0_norout n0_6_0_0x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnor1 n0_6_0x0_nor1 n0_6_0_1x0_andout n0_6_0x0_nor2 n0_6_0x0_nor2 PmosMod
        Mn0_6_0x0_nnor1 n0_6_0x0_norout n0_6_0_1x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnor2 n0_6_0x0_nor2 n0_6_0_2x0_andout n0_6_0x0_nor3 n0_6_0x0_nor3 PmosMod
        Mn0_6_0x0_nnor2 n0_6_0x0_norout n0_6_0_2x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnor3 n0_6_0x0_nor3 n0_6_0_3x0_andout VDD VDD PmosMod
        Mn0_6_0x0_nnor3 n0_6_0x0_norout n0_6_0_3x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnot n0_6_0x0_orout n0_6_0x0_norout VDD VDD PmosMod
        Mn0_6_0x0_nnot n0_6_0x0_orout n0_6_0x0_norout 0 0 NmosMod
        Rn0_6x0_connect n0_6_0x0_orout v1_2 0.001
        """;
        string s2 = 
        """
        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        
        Rn0_0_0x0_res VDD n0_0_0x0_baseout 0.001
        Rn0_0_0x1_res VDD n0_0_0x1_baseout 0.001
        Rn0_0_0x2_res VDD n0_0_0x2_baseout 0.001

        Rn0_0x0_connect n0_0_0x0_baseout n0x0_case0_0 0.001
        Rn0_0x1_connect n0_0_0x1_baseout n0x0_case0_1 0.001
        Rn0_0x2_connect n0_0_0x2_baseout n0x0_case0_2 0.001

        Rn0_1_0x0_res 0 n0_1_0x0_baseout 0.001
        Rn0_1_0x1_res VDD n0_1_0x1_baseout 0.001
        Rn0_1_0x2_res VDD n0_1_0x2_baseout 0.001
        
        Rn0_1x0_connect n0_1_0x0_baseout n0x0_case1_0 0.001
        Rn0_1x1_connect n0_1_0x1_baseout n0x0_case1_1 0.001
        Rn0_1x2_connect n0_1_0x2_baseout n0x0_case1_2 0.001
        
        Rn0_2_0x0_res VDD n0_2_0x0_baseout 0.001
        Rn0_2_0x1_res VDD n0_2_0x1_baseout 0.001
        Rn0_2_0x2_res 0 n0_2_0x2_baseout 0.001

        Rn0_2x0_connect n0_2_0x0_baseout n0x0_case2_0 0.001
        Rn0_2x1_connect n0_2_0x1_baseout n0x0_case2_1 0.001
        Rn0_2x2_connect n0_2_0x2_baseout n0x0_case2_2 0.001

        Rn0_3_0x0_res VDD n0_3_0x0_baseout 0.001
        Rn0_3_0x1_res 0 n0_3_0x1_baseout 0.001
        Rn0_3_0x2_res 0 n0_3_0x2_baseout 0.001

        Rn0_3x0_connect n0_3_0x0_baseout n0x0_case3_0 0.001
        Rn0_3x1_connect n0_3_0x1_baseout n0x0_case3_1 0.001
        Rn0_3x2_connect n0_3_0x2_baseout n0x0_case3_2 0.001

        Rn0_4_0_0_0_0x0_res selector_0 n0_4_0_0_0_0x0_baseout 0.001
        Mn0_4_0_0_0x0_p n0_4_0_0_0x0_notout n0_4_0_0_0_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_0_0x0_n n0_4_0_0_0x0_notout n0_4_0_0_0_0x0_baseout 0 0 NmosMod

        Rn0_4_0_0_1_0x0_res selector_1 n0_4_0_0_1_0x0_baseout 0.001
        Mn0_4_0_0_1x0_p n0_4_0_0_1x0_notout n0_4_0_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_0_1x0_n n0_4_0_0_1x0_notout n0_4_0_0_1_0x0_baseout 0 0 NmosMod

        Rn0_4_0_0_2x0_res n0x0_case0_0 n0_4_0_0_2x0_baseout 0.001
        Mn0_4_0_0x0_pnand0 n0_4_0_0x0_nandout n0_4_0_0_0x0_notout VDD VDD PmosMod
        Mn0_4_0_0x0_nnand0 n0_4_0_0x0_nandout n0_4_0_0_0x0_notout n0_4_0_0x0_nand1 n0_4_0_0x0_nand1 NmosMod
        Mn0_4_0_0x0_pnand1 n0_4_0_0x0_nandout n0_4_0_0_1x0_notout VDD VDD PmosMod
        Mn0_4_0_0x0_nnand1 n0_4_0_0x0_nand1 n0_4_0_0_1x0_notout n0_4_0_0x0_nand2 n0_4_0_0x0_nand2 NmosMod
        Mn0_4_0_0x0_pnand2 n0_4_0_0x0_nandout n0_4_0_0_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_0x0_nnand2 n0_4_0_0x0_nand2 n0_4_0_0_2x0_baseout 0 0 NmosMod
        Mn0_4_0_0x0_pnot n0_4_0_0x0_andout n0_4_0_0x0_nandout VDD VDD PmosMod
        Mn0_4_0_0x0_nnot n0_4_0_0x0_andout n0_4_0_0x0_nandout 0 0 NmosMod

        Rn0_4_0_1_0x0_res selector_0 n0_4_0_1_0x0_baseout 0.001
        Rn0_4_0_1_1_0x0_res selector_1 n0_4_0_1_1_0x0_baseout 0.001
        Mn0_4_0_1_1x0_p n0_4_0_1_1x0_notout n0_4_0_1_1_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_1_1x0_n n0_4_0_1_1x0_notout n0_4_0_1_1_0x0_baseout 0 0 NmosMod

        Rn0_4_0_1_2x0_res n0x0_case1_0 n0_4_0_1_2x0_baseout 0.001
        Mn0_4_0_1x0_pnand0 n0_4_0_1x0_nandout n0_4_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_1x0_nnand0 n0_4_0_1x0_nandout n0_4_0_1_0x0_baseout n0_4_0_1x0_nand1 n0_4_0_1x0_nand1 NmosMod
        Mn0_4_0_1x0_pnand1 n0_4_0_1x0_nandout n0_4_0_1_1x0_notout VDD VDD PmosMod
        Mn0_4_0_1x0_nnand1 n0_4_0_1x0_nand1 n0_4_0_1_1x0_notout n0_4_0_1x0_nand2 n0_4_0_1x0_nand2 NmosMod
        Mn0_4_0_1x0_pnand2 n0_4_0_1x0_nandout n0_4_0_1_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_1x0_nnand2 n0_4_0_1x0_nand2 n0_4_0_1_2x0_baseout 0 0 NmosMod
        Mn0_4_0_1x0_pnot n0_4_0_1x0_andout n0_4_0_1x0_nandout VDD VDD PmosMod
        Mn0_4_0_1x0_nnot n0_4_0_1x0_andout n0_4_0_1x0_nandout 0 0 NmosMod

        Rn0_4_0_2_0_0x0_res selector_0 n0_4_0_2_0_0x0_baseout 0.001
        Mn0_4_0_2_0x0_p n0_4_0_2_0x0_notout n0_4_0_2_0_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_2_0x0_n n0_4_0_2_0x0_notout n0_4_0_2_0_0x0_baseout 0 0 NmosMod

        Rn0_4_0_2_1x0_res selector_1 n0_4_0_2_1x0_baseout 0.001
        Rn0_4_0_2_2x0_res n0x0_case2_0 n0_4_0_2_2x0_baseout 0.001
        Mn0_4_0_2x0_pnand0 n0_4_0_2x0_nandout n0_4_0_2_0x0_notout VDD VDD PmosMod
        Mn0_4_0_2x0_nnand0 n0_4_0_2x0_nandout n0_4_0_2_0x0_notout n0_4_0_2x0_nand1 n0_4_0_2x0_nand1 NmosMod
        Mn0_4_0_2x0_pnand1 n0_4_0_2x0_nandout n0_4_0_2_1x0_baseout VDD VDD PmosMod
        Mn0_4_0_2x0_nnand1 n0_4_0_2x0_nand1 n0_4_0_2_1x0_baseout n0_4_0_2x0_nand2 n0_4_0_2x0_nand2 NmosMod
        Mn0_4_0_2x0_pnand2 n0_4_0_2x0_nandout n0_4_0_2_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_2x0_nnand2 n0_4_0_2x0_nand2 n0_4_0_2_2x0_baseout 0 0 NmosMod
        Mn0_4_0_2x0_pnot n0_4_0_2x0_andout n0_4_0_2x0_nandout VDD VDD PmosMod
        Mn0_4_0_2x0_nnot n0_4_0_2x0_andout n0_4_0_2x0_nandout 0 0 NmosMod

        Rn0_4_0_3_0x0_res selector_0 n0_4_0_3_0x0_baseout 0.001
        Rn0_4_0_3_1x0_res selector_1 n0_4_0_3_1x0_baseout 0.001
        Rn0_4_0_3_2x0_res n0x0_case3_0 n0_4_0_3_2x0_baseout 0.001
        Mn0_4_0_3x0_pnand0 n0_4_0_3x0_nandout n0_4_0_3_0x0_baseout VDD VDD PmosMod
        Mn0_4_0_3x0_nnand0 n0_4_0_3x0_nandout n0_4_0_3_0x0_baseout n0_4_0_3x0_nand1 n0_4_0_3x0_nand1 NmosMod
        Mn0_4_0_3x0_pnand1 n0_4_0_3x0_nandout n0_4_0_3_1x0_baseout VDD VDD PmosMod
        Mn0_4_0_3x0_nnand1 n0_4_0_3x0_nand1 n0_4_0_3_1x0_baseout n0_4_0_3x0_nand2 n0_4_0_3x0_nand2 NmosMod
        Mn0_4_0_3x0_pnand2 n0_4_0_3x0_nandout n0_4_0_3_2x0_baseout VDD VDD PmosMod
        Mn0_4_0_3x0_nnand2 n0_4_0_3x0_nand2 n0_4_0_3_2x0_baseout 0 0 NmosMod
        Mn0_4_0_3x0_pnot n0_4_0_3x0_andout n0_4_0_3x0_nandout VDD VDD PmosMod
        Mn0_4_0_3x0_nnot n0_4_0_3x0_andout n0_4_0_3x0_nandout 0 0 NmosMod

        Mn0_4_0x0_pnor0 n0_4_0x0_norout n0_4_0_0x0_andout n0_4_0x0_nor1 n0_4_0x0_nor1 PmosMod
        Mn0_4_0x0_nnor0 n0_4_0x0_norout n0_4_0_0x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnor1 n0_4_0x0_nor1 n0_4_0_1x0_andout n0_4_0x0_nor2 n0_4_0x0_nor2 PmosMod
        Mn0_4_0x0_nnor1 n0_4_0x0_norout n0_4_0_1x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnor2 n0_4_0x0_nor2 n0_4_0_2x0_andout n0_4_0x0_nor3 n0_4_0x0_nor3 PmosMod
        Mn0_4_0x0_nnor2 n0_4_0x0_norout n0_4_0_2x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnor3 n0_4_0x0_nor3 n0_4_0_3x0_andout VDD VDD PmosMod
        Mn0_4_0x0_nnor3 n0_4_0x0_norout n0_4_0_3x0_andout 0 0 NmosMod
        Mn0_4_0x0_pnot n0_4_0x0_orout n0_4_0x0_norout VDD VDD PmosMod
        Mn0_4_0x0_nnot n0_4_0x0_orout n0_4_0x0_norout 0 0 NmosMod
        Rn0_4x0_connect n0_4_0x0_orout v1_0 0.001

        Rn0_5_0_0_0_0x0_res selector_0 n0_5_0_0_0_0x0_baseout 0.001
        Mn0_5_0_0_0x0_p n0_5_0_0_0x0_notout n0_5_0_0_0_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_0_0x0_n n0_5_0_0_0x0_notout n0_5_0_0_0_0x0_baseout 0 0 NmosMod

        Rn0_5_0_0_1_0x0_res selector_1 n0_5_0_0_1_0x0_baseout 0.001
        Mn0_5_0_0_1x0_p n0_5_0_0_1x0_notout n0_5_0_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_0_1x0_n n0_5_0_0_1x0_notout n0_5_0_0_1_0x0_baseout 0 0 NmosMod

        Rn0_5_0_0_2x0_res n0x0_case0_1 n0_5_0_0_2x0_baseout 0.001
        Mn0_5_0_0x0_pnand0 n0_5_0_0x0_nandout n0_5_0_0_0x0_notout VDD VDD PmosMod
        Mn0_5_0_0x0_nnand0 n0_5_0_0x0_nandout n0_5_0_0_0x0_notout n0_5_0_0x0_nand1 n0_5_0_0x0_nand1 NmosMod
        Mn0_5_0_0x0_pnand1 n0_5_0_0x0_nandout n0_5_0_0_1x0_notout VDD VDD PmosMod
        Mn0_5_0_0x0_nnand1 n0_5_0_0x0_nand1 n0_5_0_0_1x0_notout n0_5_0_0x0_nand2 n0_5_0_0x0_nand2 NmosMod
        Mn0_5_0_0x0_pnand2 n0_5_0_0x0_nandout n0_5_0_0_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_0x0_nnand2 n0_5_0_0x0_nand2 n0_5_0_0_2x0_baseout 0 0 NmosMod
        Mn0_5_0_0x0_pnot n0_5_0_0x0_andout n0_5_0_0x0_nandout VDD VDD PmosMod
        Mn0_5_0_0x0_nnot n0_5_0_0x0_andout n0_5_0_0x0_nandout 0 0 NmosMod

        Rn0_5_0_1_0x0_res selector_0 n0_5_0_1_0x0_baseout 0.001
        Rn0_5_0_1_1_0x0_res selector_1 n0_5_0_1_1_0x0_baseout 0.001
        Mn0_5_0_1_1x0_p n0_5_0_1_1x0_notout n0_5_0_1_1_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_1_1x0_n n0_5_0_1_1x0_notout n0_5_0_1_1_0x0_baseout 0 0 NmosMod

        Rn0_5_0_1_2x0_res n0x0_case1_1 n0_5_0_1_2x0_baseout 0.001
        Mn0_5_0_1x0_pnand0 n0_5_0_1x0_nandout n0_5_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_1x0_nnand0 n0_5_0_1x0_nandout n0_5_0_1_0x0_baseout n0_5_0_1x0_nand1 n0_5_0_1x0_nand1 NmosMod
        Mn0_5_0_1x0_pnand1 n0_5_0_1x0_nandout n0_5_0_1_1x0_notout VDD VDD PmosMod
        Mn0_5_0_1x0_nnand1 n0_5_0_1x0_nand1 n0_5_0_1_1x0_notout n0_5_0_1x0_nand2 n0_5_0_1x0_nand2 NmosMod
        Mn0_5_0_1x0_pnand2 n0_5_0_1x0_nandout n0_5_0_1_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_1x0_nnand2 n0_5_0_1x0_nand2 n0_5_0_1_2x0_baseout 0 0 NmosMod
        Mn0_5_0_1x0_pnot n0_5_0_1x0_andout n0_5_0_1x0_nandout VDD VDD PmosMod
        Mn0_5_0_1x0_nnot n0_5_0_1x0_andout n0_5_0_1x0_nandout 0 0 NmosMod

        Rn0_5_0_2_0_0x0_res selector_0 n0_5_0_2_0_0x0_baseout 0.001
        Mn0_5_0_2_0x0_p n0_5_0_2_0x0_notout n0_5_0_2_0_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_2_0x0_n n0_5_0_2_0x0_notout n0_5_0_2_0_0x0_baseout 0 0 NmosMod

        Rn0_5_0_2_1x0_res selector_1 n0_5_0_2_1x0_baseout 0.001
        Rn0_5_0_2_2x0_res n0x0_case2_1 n0_5_0_2_2x0_baseout 0.001
        Mn0_5_0_2x0_pnand0 n0_5_0_2x0_nandout n0_5_0_2_0x0_notout VDD VDD PmosMod
        Mn0_5_0_2x0_nnand0 n0_5_0_2x0_nandout n0_5_0_2_0x0_notout n0_5_0_2x0_nand1 n0_5_0_2x0_nand1 NmosMod
        Mn0_5_0_2x0_pnand1 n0_5_0_2x0_nandout n0_5_0_2_1x0_baseout VDD VDD PmosMod
        Mn0_5_0_2x0_nnand1 n0_5_0_2x0_nand1 n0_5_0_2_1x0_baseout n0_5_0_2x0_nand2 n0_5_0_2x0_nand2 NmosMod
        Mn0_5_0_2x0_pnand2 n0_5_0_2x0_nandout n0_5_0_2_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_2x0_nnand2 n0_5_0_2x0_nand2 n0_5_0_2_2x0_baseout 0 0 NmosMod
        Mn0_5_0_2x0_pnot n0_5_0_2x0_andout n0_5_0_2x0_nandout VDD VDD PmosMod
        Mn0_5_0_2x0_nnot n0_5_0_2x0_andout n0_5_0_2x0_nandout 0 0 NmosMod

        Rn0_5_0_3_0x0_res selector_0 n0_5_0_3_0x0_baseout 0.001
        Rn0_5_0_3_1x0_res selector_1 n0_5_0_3_1x0_baseout 0.001
        Rn0_5_0_3_2x0_res n0x0_case3_1 n0_5_0_3_2x0_baseout 0.001
        Mn0_5_0_3x0_pnand0 n0_5_0_3x0_nandout n0_5_0_3_0x0_baseout VDD VDD PmosMod
        Mn0_5_0_3x0_nnand0 n0_5_0_3x0_nandout n0_5_0_3_0x0_baseout n0_5_0_3x0_nand1 n0_5_0_3x0_nand1 NmosMod
        Mn0_5_0_3x0_pnand1 n0_5_0_3x0_nandout n0_5_0_3_1x0_baseout VDD VDD PmosMod
        Mn0_5_0_3x0_nnand1 n0_5_0_3x0_nand1 n0_5_0_3_1x0_baseout n0_5_0_3x0_nand2 n0_5_0_3x0_nand2 NmosMod
        Mn0_5_0_3x0_pnand2 n0_5_0_3x0_nandout n0_5_0_3_2x0_baseout VDD VDD PmosMod
        Mn0_5_0_3x0_nnand2 n0_5_0_3x0_nand2 n0_5_0_3_2x0_baseout 0 0 NmosMod
        Mn0_5_0_3x0_pnot n0_5_0_3x0_andout n0_5_0_3x0_nandout VDD VDD PmosMod
        Mn0_5_0_3x0_nnot n0_5_0_3x0_andout n0_5_0_3x0_nandout 0 0 NmosMod

        Mn0_5_0x0_pnor0 n0_5_0x0_norout n0_5_0_0x0_andout n0_5_0x0_nor1 n0_5_0x0_nor1 PmosMod
        Mn0_5_0x0_nnor0 n0_5_0x0_norout n0_5_0_0x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnor1 n0_5_0x0_nor1 n0_5_0_1x0_andout n0_5_0x0_nor2 n0_5_0x0_nor2 PmosMod
        Mn0_5_0x0_nnor1 n0_5_0x0_norout n0_5_0_1x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnor2 n0_5_0x0_nor2 n0_5_0_2x0_andout n0_5_0x0_nor3 n0_5_0x0_nor3 PmosMod
        Mn0_5_0x0_nnor2 n0_5_0x0_norout n0_5_0_2x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnor3 n0_5_0x0_nor3 n0_5_0_3x0_andout VDD VDD PmosMod
        Mn0_5_0x0_nnor3 n0_5_0x0_norout n0_5_0_3x0_andout 0 0 NmosMod
        Mn0_5_0x0_pnot n0_5_0x0_orout n0_5_0x0_norout VDD VDD PmosMod
        Mn0_5_0x0_nnot n0_5_0x0_orout n0_5_0x0_norout 0 0 NmosMod
        Rn0_5x0_connect n0_5_0x0_orout v1_1 0.001

        Rn0_6_0_0_0_0x0_res selector_0 n0_6_0_0_0_0x0_baseout 0.001
        Mn0_6_0_0_0x0_p n0_6_0_0_0x0_notout n0_6_0_0_0_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_0_0x0_n n0_6_0_0_0x0_notout n0_6_0_0_0_0x0_baseout 0 0 NmosMod

        Rn0_6_0_0_1_0x0_res selector_1 n0_6_0_0_1_0x0_baseout 0.001
        Mn0_6_0_0_1x0_p n0_6_0_0_1x0_notout n0_6_0_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_0_1x0_n n0_6_0_0_1x0_notout n0_6_0_0_1_0x0_baseout 0 0 NmosMod

        Rn0_6_0_0_2x0_res n0x0_case0_2 n0_6_0_0_2x0_baseout 0.001
        Mn0_6_0_0x0_pnand0 n0_6_0_0x0_nandout n0_6_0_0_0x0_notout VDD VDD PmosMod
        Mn0_6_0_0x0_nnand0 n0_6_0_0x0_nandout n0_6_0_0_0x0_notout n0_6_0_0x0_nand1 n0_6_0_0x0_nand1 NmosMod
        Mn0_6_0_0x0_pnand1 n0_6_0_0x0_nandout n0_6_0_0_1x0_notout VDD VDD PmosMod
        Mn0_6_0_0x0_nnand1 n0_6_0_0x0_nand1 n0_6_0_0_1x0_notout n0_6_0_0x0_nand2 n0_6_0_0x0_nand2 NmosMod
        Mn0_6_0_0x0_pnand2 n0_6_0_0x0_nandout n0_6_0_0_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_0x0_nnand2 n0_6_0_0x0_nand2 n0_6_0_0_2x0_baseout 0 0 NmosMod
        Mn0_6_0_0x0_pnot n0_6_0_0x0_andout n0_6_0_0x0_nandout VDD VDD PmosMod
        Mn0_6_0_0x0_nnot n0_6_0_0x0_andout n0_6_0_0x0_nandout 0 0 NmosMod

        Rn0_6_0_1_0x0_res selector_0 n0_6_0_1_0x0_baseout 0.001
        Rn0_6_0_1_1_0x0_res selector_1 n0_6_0_1_1_0x0_baseout 0.001
        Mn0_6_0_1_1x0_p n0_6_0_1_1x0_notout n0_6_0_1_1_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_1_1x0_n n0_6_0_1_1x0_notout n0_6_0_1_1_0x0_baseout 0 0 NmosMod

        Rn0_6_0_1_2x0_res n0x0_case1_2 n0_6_0_1_2x0_baseout 0.001
        Mn0_6_0_1x0_pnand0 n0_6_0_1x0_nandout n0_6_0_1_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_1x0_nnand0 n0_6_0_1x0_nandout n0_6_0_1_0x0_baseout n0_6_0_1x0_nand1 n0_6_0_1x0_nand1 NmosMod
        Mn0_6_0_1x0_pnand1 n0_6_0_1x0_nandout n0_6_0_1_1x0_notout VDD VDD PmosMod
        Mn0_6_0_1x0_nnand1 n0_6_0_1x0_nand1 n0_6_0_1_1x0_notout n0_6_0_1x0_nand2 n0_6_0_1x0_nand2 NmosMod
        Mn0_6_0_1x0_pnand2 n0_6_0_1x0_nandout n0_6_0_1_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_1x0_nnand2 n0_6_0_1x0_nand2 n0_6_0_1_2x0_baseout 0 0 NmosMod
        Mn0_6_0_1x0_pnot n0_6_0_1x0_andout n0_6_0_1x0_nandout VDD VDD PmosMod
        Mn0_6_0_1x0_nnot n0_6_0_1x0_andout n0_6_0_1x0_nandout 0 0 NmosMod

        Rn0_6_0_2_0_0x0_res selector_0 n0_6_0_2_0_0x0_baseout 0.001
        Mn0_6_0_2_0x0_p n0_6_0_2_0x0_notout n0_6_0_2_0_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_2_0x0_n n0_6_0_2_0x0_notout n0_6_0_2_0_0x0_baseout 0 0 NmosMod

        Rn0_6_0_2_1x0_res selector_1 n0_6_0_2_1x0_baseout 0.001
        Rn0_6_0_2_2x0_res n0x0_case2_2 n0_6_0_2_2x0_baseout 0.001
        Mn0_6_0_2x0_pnand0 n0_6_0_2x0_nandout n0_6_0_2_0x0_notout VDD VDD PmosMod
        Mn0_6_0_2x0_nnand0 n0_6_0_2x0_nandout n0_6_0_2_0x0_notout n0_6_0_2x0_nand1 n0_6_0_2x0_nand1 NmosMod
        Mn0_6_0_2x0_pnand1 n0_6_0_2x0_nandout n0_6_0_2_1x0_baseout VDD VDD PmosMod
        Mn0_6_0_2x0_nnand1 n0_6_0_2x0_nand1 n0_6_0_2_1x0_baseout n0_6_0_2x0_nand2 n0_6_0_2x0_nand2 NmosMod
        Mn0_6_0_2x0_pnand2 n0_6_0_2x0_nandout n0_6_0_2_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_2x0_nnand2 n0_6_0_2x0_nand2 n0_6_0_2_2x0_baseout 0 0 NmosMod
        Mn0_6_0_2x0_pnot n0_6_0_2x0_andout n0_6_0_2x0_nandout VDD VDD PmosMod
        Mn0_6_0_2x0_nnot n0_6_0_2x0_andout n0_6_0_2x0_nandout 0 0 NmosMod

        Rn0_6_0_3_0x0_res selector_0 n0_6_0_3_0x0_baseout 0.001
        Rn0_6_0_3_1x0_res selector_1 n0_6_0_3_1x0_baseout 0.001
        Rn0_6_0_3_2x0_res n0x0_case3_2 n0_6_0_3_2x0_baseout 0.001
        Mn0_6_0_3x0_pnand0 n0_6_0_3x0_nandout n0_6_0_3_0x0_baseout VDD VDD PmosMod
        Mn0_6_0_3x0_nnand0 n0_6_0_3x0_nandout n0_6_0_3_0x0_baseout n0_6_0_3x0_nand1 n0_6_0_3x0_nand1 NmosMod
        Mn0_6_0_3x0_pnand1 n0_6_0_3x0_nandout n0_6_0_3_1x0_baseout VDD VDD PmosMod
        Mn0_6_0_3x0_nnand1 n0_6_0_3x0_nand1 n0_6_0_3_1x0_baseout n0_6_0_3x0_nand2 n0_6_0_3x0_nand2 NmosMod
        Mn0_6_0_3x0_pnand2 n0_6_0_3x0_nandout n0_6_0_3_2x0_baseout VDD VDD PmosMod
        Mn0_6_0_3x0_nnand2 n0_6_0_3x0_nand2 n0_6_0_3_2x0_baseout 0 0 NmosMod
        Mn0_6_0_3x0_pnot n0_6_0_3x0_andout n0_6_0_3x0_nandout VDD VDD PmosMod
        Mn0_6_0_3x0_nnot n0_6_0_3x0_andout n0_6_0_3x0_nandout 0 0 NmosMod

        Mn0_6_0x0_pnor0 n0_6_0x0_norout n0_6_0_0x0_andout n0_6_0x0_nor1 n0_6_0x0_nor1 PmosMod
        Mn0_6_0x0_nnor0 n0_6_0x0_norout n0_6_0_0x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnor1 n0_6_0x0_nor1 n0_6_0_1x0_andout n0_6_0x0_nor2 n0_6_0x0_nor2 PmosMod
        Mn0_6_0x0_nnor1 n0_6_0x0_norout n0_6_0_1x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnor2 n0_6_0x0_nor2 n0_6_0_2x0_andout n0_6_0x0_nor3 n0_6_0x0_nor3 PmosMod
        Mn0_6_0x0_nnor2 n0_6_0x0_norout n0_6_0_2x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnor3 n0_6_0x0_nor3 n0_6_0_3x0_andout VDD VDD PmosMod
        Mn0_6_0x0_nnor3 n0_6_0x0_norout n0_6_0_3x0_andout 0 0 NmosMod
        Mn0_6_0x0_pnot n0_6_0x0_orout n0_6_0x0_norout VDD VDD PmosMod
        Mn0_6_0x0_nnot n0_6_0x0_orout n0_6_0x0_norout 0 0 NmosMod
        Rn0_6x0_connect n0_6_0x0_orout v1_2 0.001
        """;
    
        string[] s1Lines = s1.Split('\n');
        string[] s2Lines = s2.Split('\n');
        int s1Counter = 0;
        int s2Counter = 0;

        while (s1Counter < s1Lines.Length && s2Counter < s2Lines.Length)
        {
            while (s1Lines[s1Counter].Trim() == "")
            {
                s1Counter++;
                if (s1Counter >= s1Lines.Length)
                    return;
            }
            while (s2Lines[s2Counter].Trim() == "")
            {
                s2Counter++;
                if (s2Counter >= s2Lines.Length)
                    return;
            }
            string s1Line = s1Lines[s1Counter++];
            string s2Line = s2Lines[s2Counter++];
            if (s1Line != s2Line)
            {
                Console.WriteLine($"S1: {s1Line}");
                Console.WriteLine($"S2: {s2Line}");
                break;
            }
        }
    }
}