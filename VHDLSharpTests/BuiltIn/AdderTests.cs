using SpiceSharp.Simulations;
using VHDLSharp.BuiltIn;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
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
        ISimulationResult yResult = results[3];
        ISimulationResult coutResult = results[4];

        // Check y results
        for (int i = 0; i < yResult.TimeSteps.Length; i++)
        {
            int? expectedResult = yResult.TimeSteps[i] switch
            {
                >        timeBuffer and < 1e-5 - timeBuffer => 0,
                > 1e-5 + timeBuffer and < 3e-5 - timeBuffer => 1,
                > 3e-5 + timeBuffer and < 4e-5 - timeBuffer => 0,
                > 4e-5 + timeBuffer and < 5e-5 - timeBuffer => 1,
                > 5e-5 + timeBuffer and < 7e-5 - timeBuffer => 0,
                > 7e-5 + timeBuffer and < 8e-5 - timeBuffer => 1,
                _ => null,
            };

            if (expectedResult is not null)
                Assert.AreEqual(expectedResult, yResult.Values[i]);
        }
        
        // Check cout results
        for (int i = 0; i < coutResult.TimeSteps.Length; i++)
        {
            int? expectedResult = coutResult.TimeSteps[i] switch
            {
                >        timeBuffer and < 3e-5 - timeBuffer => 0,
                > 3e-5 + timeBuffer and < 4e-5 - timeBuffer => 1,
                > 4e-5 + timeBuffer and < 5e-5 - timeBuffer => 0,
                > 5e-5 + timeBuffer and < 8e-5 - timeBuffer => 1,
                _ => null,
            };

            if (expectedResult is not null)
                Assert.AreEqual(expectedResult, coutResult.Values[i]);
        }
    }
}