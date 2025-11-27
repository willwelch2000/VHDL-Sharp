using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Signals.Derived;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

// 24 seconds!
[TestClass]
public class LogicSignalTests
{
    private const double timeBuffer = 5e-7;

    [TestMethod]
    // Uses a LogicSignal and then uses that in an AddedSignal
    public void TestNestedSignal()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Vector s1 = module.GenerateVector("s1", 2);
        Vector s2 = module.GenerateVector("s2", 2);
        Vector s3 = module.GenerateVector("s3", 2);
        Vector s4 = module.GenerateVector("s4", 2);
        s4.AssignBehavior(s1.And(s2).ToSignal().Plus(s3));
        Port s1p = module.AddNewPort(s1, PortDirection.Input);
        Port s2p = module.AddNewPort(s2, PortDirection.Input);
        Port s3p = module.AddNewPort(s3, PortDirection.Input);
        module.AddNewPort(s4, PortDirection.Output);

        string vhdl = module.GetVhdl();
        string expectedVhdl =
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity mod1 is
            port (
                s1	: in	std_logic_vector(1 downto 0);
                s2	: in	std_logic_vector(1 downto 0);
                s3	: in	std_logic_vector(1 downto 0);
                s4	: out	std_logic_vector(1 downto 0)
            );
        end mod1;

        architecture rtl of mod1 is
            signal DerivedSignal0	: std_logic_vector(1 downto 0)
            signal DerivedSignal1	: std_logic_vector(1 downto 0)

            component Adder_2bits_noCIn_noCOut
                port (
                    A	: in	std_logic_vector(1 downto 0);
                    B	: in	std_logic_vector(1 downto 0);
                    Y	: out	std_logic_vector(1 downto 0)
                );
            end component Adder_2bits_noCIn_noCOut;
            
            component DerivedModule1
                port (
                    Output	: out	std_logic_vector(1 downto 0);
                    Input0	: in	std_logic_vector(1 downto 0);
                    Input1	: in	std_logic_vector(1 downto 0)
                );
            end component DerivedModule1;
            
        begin
            DerivedInstance0 : Adder_2bits_noCIn_noCOut
                port map (
                    A => DerivedSignal1,
                    B => s3,
                    Y => DerivedSignal0
                );
            
            DerivedInstance1 : DerivedModule1
                port map (
                    Output => DerivedSignal1,
                    Input0 => s1,
                    Input1 => s2
                );
            
            s4 <= DerivedSignal0;
        end rtl;

        entity Adder_2bits_noCIn_noCOut is
            port (
                A	: in	std_logic_vector(1 downto 0);
                B	: in	std_logic_vector(1 downto 0);
                Y	: out	std_logic_vector(1 downto 0)
            );
        end Adder_2bits_noCIn_noCOut;

        architecture rtl of Adder_2bits_noCIn_noCOut is
            signal COut0	: std_logic

            component Adder_1bit_noCIn
                port (
                    A	: in	std_logic;
                    B	: in	std_logic;
                    Y	: out	std_logic;
                    COut	: out	std_logic
                );
            end component Adder_1bit_noCIn;
            
            component Adder_1bit_noCOut
                port (
                    A	: in	std_logic;
                    B	: in	std_logic;
                    Y	: out	std_logic;
                    CIn	: in	std_logic
                );
            end component Adder_1bit_noCOut;
            
        begin
            Adder0 : Adder_1bit_noCIn
                port map (
                    A => A(0),
                    B => B(0),
                    Y => Y(0),
                    COut => COut0
                );
            
            Adder1 : Adder_1bit_noCOut
                port map (
                    A => A(1),
                    B => B(1),
                    Y => Y(1),
                    CIn => COut0
                );
            
        end rtl;

        entity Adder_1bit_noCIn is
            port (
                A	: in	std_logic;
                B	: in	std_logic;
                Y	: out	std_logic;
                COut	: out	std_logic
            );
        end Adder_1bit_noCIn;

        architecture rtl of Adder_1bit_noCIn is
        begin
            Y <= ((A or B) and (not ((A and B))));
            COut <= (A and B);
        end rtl;

        entity Adder_1bit_noCOut is
            port (
                A	: in	std_logic;
                B	: in	std_logic;
                Y	: out	std_logic;
                CIn	: in	std_logic
            );
        end Adder_1bit_noCOut;

        architecture rtl of Adder_1bit_noCOut is
            signal aXorB	: std_logic
        begin
            aXorB <= ((A or B) and (not ((A and B))));
            Y <= ((aXorB or CIn) and (not ((aXorB and CIn))));
        end rtl;

        entity DerivedModule1 is
            port (
                Output	: out	std_logic_vector(1 downto 0);
                Input0	: in	std_logic_vector(1 downto 0);
                Input1	: in	std_logic_vector(1 downto 0)
            );
        end DerivedModule1;

        architecture rtl of DerivedModule1 is
        begin
            Output <= (Input0 and Input1);
        end rtl;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Run simulation
        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 64e-5
        };
        simulation.StimulusMapping[s1p] = new MultiDimensionalStimulus([
            new PulseStimulus(1e-5, 1e-5, 2e-5),
            new PulseStimulus(2e-5, 2e-5, 4e-5),
        ]);
        simulation.StimulusMapping[s2p] = new MultiDimensionalStimulus([
            new PulseStimulus(4e-5, 4e-5, 8e-5),
            new PulseStimulus(8e-5, 8e-5, 16e-5),
        ]);
        simulation.StimulusMapping[s3p] = new MultiDimensionalStimulus([
            new PulseStimulus(16e-5, 16e-5, 32e-5),
            new PulseStimulus(32e-5, 32e-5, 64e-5),
        ]);

        SubcircuitReference moduleRef = new(module, []);
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

            int sum = (s1Results.Values[i] & s2Results.Values[i]) + s3Results.Values[i];
            int expectedS4 = sum % 4;
            Assert.AreEqual(expectedS4, s4Results.Values[i]);
        }
    }
}