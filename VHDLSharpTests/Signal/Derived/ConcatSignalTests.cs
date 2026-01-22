using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Signals.Derived;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class ConcatSignalTests
{
    private const double timeBuffer = 5e-7;

    [TestMethod]
    public void BasicTest()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Vector s1 = module.GenerateVector("s1", 2);
        Signal s2 = module.GenerateSignal("s2");
        Vector s3 = module.GenerateVector("s3", 3);
        s3.AssignBehavior(s1.ConcatWith(s2));
        Port s1p = module.AddNewPort(s1, PortDirection.Input);
        Port s2p = module.AddNewPort(s2, PortDirection.Input);
        module.AddNewPort(s3, PortDirection.Output);

        string vhdl = module.GetVhdl();
        string expectedVhdl =
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity mod1 is
            port (
                s1	: in	std_logic_vector(1 downto 0);
                s2	: in	std_logic;
                s3	: out	std_logic_vector(2 downto 0)
            );
        end mod1;

        architecture rtl of mod1 is
            signal DerivedSignal0	: std_logic_vector(2 downto 0)

            component DerivedModule0
                port (
                    Upper	: in	std_logic_vector(1 downto 0);
                    Lower	: in	std_logic;
                    Output	: out	std_logic_vector(2 downto 0)
                );
            end component DerivedModule0;
            
        begin
            DerivedInstance0 : DerivedModule0
                port map (
                    Upper => s1,
                    Lower => s2,
                    Output => DerivedSignal0
                );
            
            s3 <= DerivedSignal0;
        end rtl;

        entity DerivedModule0 is
            port (
                Upper	: in	std_logic_vector(1 downto 0);
                Lower	: in	std_logic;
                Output	: out	std_logic_vector(2 downto 0)
            );
        end DerivedModule0;

        architecture rtl of DerivedModule0 is
        begin
            Output(0) <= Lower;
            Output(2 downto 1) <= Upper;
        end rtl;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Run simulation
        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 8e-5
        };
        simulation.StimulusMapping[s1p] = new MultiDimensionalStimulus([
            new PulseStimulus(1e-5, 1e-5, 2e-5),
            new PulseStimulus(2e-5, 2e-5, 4e-5),
        ]);
        simulation.StimulusMapping[s2p] = new PulseStimulus(4e-5, 4e-5, 8e-5);

        SubmoduleReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s1));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s2));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s3));
        ISimulationResult[] results = [.. simulation.Simulate()];
        ISimulationResult s1Results = results[0];
        ISimulationResult s2Results = results[1];
        ISimulationResult s3Results = results[2];

        // Check results
        for (int i = 0; i < s1Results.TimeSteps.Length; i++)
        {
            double timeStep = s1Results.TimeSteps[i];
            // Checks if we're within timeBuffer of a transition point
            if (Math.Abs(timeStep - 1e-5 * Math.Round(timeStep / 1e-5, 0)) < timeBuffer)
                continue;

            int expectedS3 = s1Results.Values[i]*2 + s2Results.Values[i]; // s1 is left-shifted 1 bit
            Assert.AreEqual(expectedS3, s3Results.Values[i]);
        }
    }
}