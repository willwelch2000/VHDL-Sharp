using VHDLSharp.Modules;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;
using VHDLSharpTests;

namespace VHDLSharp.Signals.Derived;

[TestClass]
public class ExtendedSignalTests
{
    private const double timeBuffer = 5e-7;

    [TestMethod]
    public void BasicTest()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Vector s2 = module.GenerateVector("s2", 2);
        Vector s3 = module.GenerateVector("s3", 4);
        Vector s4 = module.GenerateVector("s4", 5);
        Port s1p = module.AddNewPort(s1, PortDirection.Input);
        Port s2p = module.AddNewPort(s2, PortDirection.Input);
        module.AddNewPort(s3, PortDirection.Output);
        module.AddNewPort(s4, PortDirection.Output);

        // Assign s3 normally, s4 as linked signal
        s3.AssignBehavior(s1.Extend(4));
        ExtendedSignal extensionS2 = new(s2, 5, true)
        {
            LinkedSignal = s4
        };

        string vhdl = module.GetVhdl();
        string expectedVhdl =
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity mod1 is
            port (
                s1	: in	std_logic;
                s2	: in	std_logic_vector(1 downto 0);
                s3	: out	std_logic_vector(3 downto 0);
                s4	: out	std_logic_vector(4 downto 0)
            );
        end mod1;

        architecture rtl of mod1 is
            signal DerivedSignal0	: std_logic_vector(3 downto 0)

            component Extension_1_4
                port (
                    Input	: in	std_logic;
                    Output	: out	std_logic_vector(3 downto 0)
                );
            end component Extension_1_4;
            
            component Extension_2_5_signed
                port (
                    Input	: in	std_logic_vector(1 downto 0);
                    Output	: out	std_logic_vector(4 downto 0)
                );
            end component Extension_2_5_signed;
            
        begin
            DerivedInstance0 : Extension_1_4
                port map (
                    Input => s1,
                    Output => DerivedSignal0
                );
            
            DerivedInstance1 : Extension_2_5_signed
                port map (
                    Input => s2,
                    Output => s4
                );
            
            s3 <= DerivedSignal0;
        end rtl;

        entity Extension_1_4 is
            port (
                Input	: in	std_logic;
                Output	: out	std_logic_vector(3 downto 0)
            );
        end Extension_1_4;

        architecture rtl of Extension_1_4 is
        begin
            Output(0) <= Input;
            Output(3 downto 1) <= "000";
        end rtl;

        entity Extension_2_5_signed is
            port (
                Input	: in	std_logic_vector(1 downto 0);
                Output	: out	std_logic_vector(4 downto 0)
            );
        end Extension_2_5_signed;

        architecture rtl of Extension_2_5_signed is
        begin
            Output(1 downto 0) <= Input;
            Output(2) <= Input(1);
            Output(3) <= Input(1);
            Output(4) <= Input(1);
        end rtl;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Run simulation
        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 8e-5
        };
        simulation.StimulusMapping[s1p] = new PulseStimulus(1e-5, 1e-5, 2e-5);
        simulation.StimulusMapping[s2p] = new MultiDimensionalStimulus([
            new PulseStimulus(2e-5, 2e-5, 4e-5),
            new PulseStimulus(4e-5, 4e-5, 8e-5),
        ]);

        SubmoduleReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s1));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s2));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s3));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s4));
        ISimulationResult[] results = [.. simulation.Simulate()];
        ISimulationResult s1Results = results[0];
        ISimulationResult s2Results = results[1];
        ISimulationResult s3Results = results[2];
        ISimulationResult s4Results = results[3];

        // Check results
        for (int i = 0; i < s1Results.TimeSteps.Length; i++)
        {
            double timeStep = s1Results.TimeSteps[i];
            // Checks if we're within timeBuffer of a transition point
            if (Math.Abs(timeStep - 1e-5 * Math.Round(timeStep / 1e-5, 0)) < timeBuffer)
                continue;

            int s2Val = s2Results.Values[i];
            int expectedS3 = s1Results.Values[i];
            int expectedS4 = s2Val > 1 ? s2Val + 4 + 8 + 16 : s2Val;

            Assert.AreEqual(expectedS3, s3Results.Values[i]);
            Assert.AreEqual(expectedS4, s4Results.Values[i]);
        }
    }
}