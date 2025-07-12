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
        Module module = new("module");
        IModule adder = new Adder(1);

        Port a = module.AddNewPort("A", PortDirection.Input);
        Port b = module.AddNewPort("B", PortDirection.Input);
        Port cin = module.AddNewPort("CIn", PortDirection.Input);
        Port y = module.AddNewPort("Y", PortDirection.Output);
        Port cout = module.AddNewPort("Cout", PortDirection.Output);

        Instantiation inst = module.AddNewInstantiation(adder, "Adder");
        inst.PortMapping.SetPort("A", a.Signal);
        inst.PortMapping.SetPort("B", b.Signal);
        inst.PortMapping.SetPort("CIn", cin.Signal);
        inst.PortMapping.SetPort("Y", y.Signal);
        inst.PortMapping.SetPort("COut", cout.Signal);

        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 8e-5,
        };
        simulation.StimulusMapping[a] = new PulseStimulus(1e-5, 1e-5, 2e-5);
        simulation.StimulusMapping[b] = new PulseStimulus(2e-5, 2e-5, 4e-5);
        simulation.StimulusMapping[cin] = new PulseStimulus(4e-5, 4e-5, 8e-5);

        SubcircuitReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(a.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(b.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cin.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(y.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cout.Signal));
        ISimulationResult[] results = [.. simulation.Simulate()];
        TestResults(results, 1);
    }

    [TestMethod]
    public void Adder2BitTest()
    {
        Module module = new("module");
        IModule adder = new Adder(2);

        Port a = module.AddNewPort("A", 2, PortDirection.Input);
        Port b = module.AddNewPort("B", 2, PortDirection.Input);
        Port cin = module.AddNewPort("CIn", PortDirection.Input);
        Port y = module.AddNewPort("Y", 2, PortDirection.Output);
        Port cout = module.AddNewPort("Cout", PortDirection.Output);

        Instantiation inst = module.AddNewInstantiation(adder, "Adder");
        inst.PortMapping.SetPort("A", a.Signal);
        inst.PortMapping.SetPort("B", b.Signal);
        inst.PortMapping.SetPort("CIn", cin.Signal);
        inst.PortMapping.SetPort("Y", y.Signal);
        inst.PortMapping.SetPort("COut", cout.Signal);

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
        simulation.StimulusMapping[cin] = new PulseStimulus(16e-5, 16e-5, 32e-5);

        SubcircuitReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(a.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(b.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cin.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(y.Signal));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(cout.Signal));
        ISimulationResult[] results = [.. simulation.Simulate()];
        TestResults(results, 2);
    }

    private static void TestResults(ISimulationResult[] results, int bits)
    {
        ISimulationResult aResults = results[0];
        ISimulationResult bResults = results[1];
        ISimulationResult cinResults = results[2];
        ISimulationResult yResults = results[3];
        ISimulationResult coutResults = results[4];

        // Check results--all timesteps should be the same
        for (int i = 0; i < yResults.TimeSteps.Length; i++)
        {
            double timeStep = yResults.TimeSteps[i];
            if (Math.Abs(timeStep - 1e-5*Math.Round(timeStep / 1e-5, 0)) < timeBuffer)
                continue;

            int expectedY = (aResults.Values[i] + bResults.Values[i] + cinResults.Values[i]) % (1<<bits);
            int expectedCout = (aResults.Values[i] + bResults.Values[i] + cinResults.Values[i]) / (1<<bits);
            Assert.AreEqual(expectedY, yResults.Values[i]);
            Assert.AreEqual(expectedCout, coutResults.Values[i]);
        }
    }
}