using ScottPlot;
using ScottPlot.Plottables;
using VHDLSharp.BuiltIn;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharp;

public static class GenerateFigures
{
    public static Multiplot DffFigure()
    {
        IModule dff = new DFlipFlop();
        Module mainModule = new("Main");
        Port clk = mainModule.AddNewPort("CLK", PortDirection.Input);
        Port d = mainModule.AddNewPort("D", PortDirection.Input);
        Port q = mainModule.AddNewPort("Q", PortDirection.Output);
        Instantiation dffInst = mainModule.AddNewInstantiation(dff, "DFF");
        dffInst.PortMapping.SetPort("CLK", clk.Signal);
        dffInst.PortMapping.SetPort("D", d.Signal);
        dffInst.PortMapping.SetPort("Q", q.Signal);

        RuleBasedSimulation simulation = new(mainModule, new DefaultTimeStepGenerator())
        {
            Length = 10e-5,
        };
        SubcircuitReference subcircuit = new(mainModule, []);
        SignalReference clkRef = subcircuit.GetChildSignalReference(clk.Signal);
        SignalReference dRef = subcircuit.GetChildSignalReference(d.Signal);
        SignalReference qRef = subcircuit.GetChildSignalReference(q.Signal);
        simulation.SignalsToMonitor.Add(clkRef);
        simulation.SignalsToMonitor.Add(dRef);
        simulation.SignalsToMonitor.Add(qRef);

        simulation.AssignStimulus(clk, new PulseStimulus(1e-5, 1e-5, 2e-5));
        simulation.AssignStimulus(d, new PulseStimulus(2e-5, 2e-5, 4e-5));
        ISimulationResult[] results = [.. simulation.Simulate()];
        return CreatePlot(results);
    }

    public static Multiplot Addition2Bit(bool carryIn = true, bool carryOut = true)
    {
        Module module = new("module");
        IModule adder = new Adder(2, carryIn, carryOut);

        Port a = module.AddNewPort("A", 2, PortDirection.Input);
        Port b = module.AddNewPort("B", 2, PortDirection.Input);
        Port? cin = carryIn ? module.AddNewPort("CIn", PortDirection.Input) : null;
        Port y = module.AddNewPort("Y", 2, PortDirection.Output);
        Port? cout = carryOut ? module.AddNewPort("Cout", PortDirection.Output) : null;

        Instantiation inst = module.AddNewInstantiation(adder, "Adder");
        inst.PortMapping.SetPort("A", a.Signal);
        inst.PortMapping.SetPort("B", b.Signal);
        if (carryIn)
            inst.PortMapping.SetPort("CIn", cin!.Signal);
        inst.PortMapping.SetPort("Y", y.Signal);
        if (carryOut)
            inst.PortMapping.SetPort("COut", cout!.Signal);

        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 32e-5,
        };
        simulation.StimulusMapping[a] = new MultiDimensionalStepStimulus(1e-5, 2);
        simulation.StimulusMapping[b] = new MultiDimensionalStepStimulus(4e-5, 2);
        if (carryIn)
            simulation.StimulusMapping[cin!] = new PulseStimulus(16e-5, 16e-5, 32e-5);

        SubcircuitReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(a.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(b.Signal));
        if (carryIn)
            simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cin!.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(y.Signal));
        if (carryOut)
            simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cout!.Signal));
        ISimulationResult[] results = [.. simulation.Simulate()];
        return CreatePlot(results);
    }

    public static Multiplot ShiftRegister()
    {
        Module module = new("module");
        Signals.Signal clk = module.GenerateSignal("CLK");
        Signals.Signal s1 = module.GenerateSignal("S1");
        Signals.Signal s2 = module.GenerateSignal("S2");
        Signals.Signal s3 = module.GenerateSignal("S3");
        Signals.Signal s4 = module.GenerateSignal("S4");
        Port pClk = module.AddNewPort(clk, PortDirection.Input);
        Port p1 = module.AddNewPort(s1, PortDirection.Input);
        module.AddNewPort(s2, PortDirection.Output);
        module.AddNewPort(s3, PortDirection.Output);
        module.AddNewPort(s4, PortDirection.Output);

        IModule dff = new DFlipFlop();
        IEnumerable<(Signals.Signal, Signals.Signal)> path = [(s1, s2), (s2, s3), (s3, s4)];
        int i = 0;
        foreach ((Signals.Signal d, Signals.Signal q) in path)
        {
            Instantiation inst = module.AddNewInstantiation(dff, $"Inst{i++}");
            inst.PortMapping.SetPort("D", d);
            inst.PortMapping.SetPort("Q", q);
            inst.PortMapping.SetPort("CLK", clk);
        }

        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator())
        {
            Length = 32e-5,
        };
        simulation.StimulusMapping[pClk] = new PulseStimulus(1e-5, 1e-5, 2e-5);
        simulation.StimulusMapping[p1] = new TimeDefinedStimulus()
        {
            Points = new()
            {
                {0, false}, {4e-5, true}, {10e-5, false}, {12e-5, true}, {14e-5, false}, {16e-5, true},
            }
        };

        SubcircuitReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(clk));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s1));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s2));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s3));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s4));
        ISimulationResult[] results = [.. simulation.Simulate()];
        return CreatePlot(results);
    }

    public static Multiplot CreatePlot(ISimulationResult[] results)
    {
        Color[] colors = [Color.FromHex("#CCCCFF"), Colors.LightBlue, Colors.IndianRed, Colors.Green, Colors.Purple, Colors.Coral];
        int i = 0;
        Multiplot multiplot = new();
        foreach (ISimulationResult result in results)
        {
            Plot plot = multiplot.AddPlot();
            Scatter scatter = plot.Add.Scatter(result.TimeSteps, result.Values, colors[i++ % colors.Length]);
            scatter.LineWidth = 5;
            scatter.LegendText = result.SignalReference.Signal.Name;
        }
        return multiplot;
    }
}