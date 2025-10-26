using VHDLSharp.BuiltIn;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class AdderTests
{
    private const double timeBuffer = 5e-7;

    [TestMethod]
    public void Adder1BitTest()
    {
        for (int i = 0; i < 4; i++)
        {
            bool carryIn = i > 2;
            bool carryOut = i % 2 == 1;
            Module module = new("module");
            IModule adder = new Adder(1, carryIn, carryOut);

            Port a = module.AddNewPort("A", PortDirection.Input);
            Port b = module.AddNewPort("B", PortDirection.Input);
            Port? cin = carryIn ? module.AddNewPort("CIn", PortDirection.Input) : null;
            Port y = module.AddNewPort("Y", PortDirection.Output);
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
                Length = 8e-5,
            };
            simulation.StimulusMapping[a] = new PulseStimulus(1e-5, 1e-5, 2e-5);
            simulation.StimulusMapping[b] = new PulseStimulus(2e-5, 2e-5, 4e-5);
            if (carryIn)
                simulation.StimulusMapping[cin!] = new PulseStimulus(4e-5, 4e-5, 8e-5);

            SubcircuitReference moduleRef = new(module, []);
            simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(a.Signal));
            simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(b.Signal));
            if (carryIn)
                simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cin!.Signal));
            simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(y.Signal));
            if (carryOut)
                simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cout!.Signal));
            ISimulationResult[] results = [.. simulation.Simulate()];
            TestResults(results, 1, carryIn, carryOut);
        }
    }

    [TestMethod]
    public void Adder2BitTest()
    {
        // All combinations of carry-in and carry-out
        for (int i = 0; i < 4; i++)
        {
            bool carryIn = i > 2;
            bool carryOut = i % 2 == 1;
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
            simulation.StimulusMapping[a] = new MultiDimensionalStimulus([
                new PulseStimulus(1e-5, 1e-5, 2e-5),
                new PulseStimulus(2e-5, 2e-5, 4e-5),
            ]);
            simulation.StimulusMapping[b] = new MultiDimensionalStimulus([
                new PulseStimulus(4e-5, 4e-5, 8e-5),
                new PulseStimulus(8e-5, 8e-5, 16e-5),
            ]);
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
            TestResults(results, 2, carryIn, carryOut);
        }
    }

    public static void TestResults(ISimulationResult[] results, int bits, bool carryIn, bool carryOut)
    {
        ISimulationResult aResults = results[0];
        ISimulationResult bResults = results[1];
        ISimulationResult? cinResults = carryIn ? results[2] : null;
        ISimulationResult yResults = carryIn ? results[3] : results[2];
        ISimulationResult? coutResults = carryOut ? results[^1] : null;

        // Check results--all timesteps should be the same
        for (int i = 0; i < yResults.TimeSteps.Length; i++)
        {
            double timeStep = yResults.TimeSteps[i];
            if (Math.Abs(timeStep - 1e-5*Math.Round(timeStep / 1e-5, 0)) < timeBuffer)
                continue;

            int expectedY = (carryIn ? (aResults.Values[i] + bResults.Values[i] + cinResults!.Values[i]) :
                (aResults.Values[i] + bResults.Values[i])) % (1<<bits);
            Assert.AreEqual(expectedY, yResults.Values[i]);
            if (carryOut)
            {
                int expectedCout = (carryIn ? (aResults.Values[i] + bResults.Values[i] + cinResults!.Values[i]) :
                    (aResults.Values[i] + bResults.Values[i])) / (1 << bits);
                Assert.AreEqual(expectedCout, coutResults!.Values[i]);
            }
        }
    }
}