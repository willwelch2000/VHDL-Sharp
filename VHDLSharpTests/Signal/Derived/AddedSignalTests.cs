using SpiceSharp.Components;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class AddedSignalTests
{
    [TestMethod]
    public void Test1bit()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Signal s2 = module.GenerateSignal("s2");
        Signal s3 = module.GenerateSignal("s3");
        s3.AssignBehavior(s1.Plus(s2));
        Port s1p = module.AddNewPort(s1, PortDirection.Input);
        Port s2p = module.AddNewPort(s2, PortDirection.Input);
        module.AddNewPort(s3, PortDirection.Output);

        // Check VHDL
        string vhdl = module.GetVhdl();
        string expectedVhdl =
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity mod1 is
            port (
                s1	: in	std_logic;
                s2	: in	std_logic;
                s3	: out	std_logic
            );
        end mod1;

        architecture rtl of mod1 is
            signal DerivedSignal0	: std_logic
        component Adder_1bit_noCIn_noCOut
            port (
                A	: in	std_logic;
                B	: in	std_logic;
                Y	: out	std_logic
            );
        end component Adder_1bit_noCIn_noCOut;

        begin
            DerivedInstance0 : Adder_1bit_noCIn_noCOut
                port map (
                    A => s1,
                    B => s2,
                    Y => DerivedSignal0
                );
            
            s3 <= DerivedSignal0;
        end rtl;

        entity Adder_1bit_noCIn_noCOut is
            port (
                A	: in	std_logic;
                B	: in	std_logic;
                Y	: out	std_logic
            );
        end Adder_1bit_noCIn_noCOut;

        architecture rtl of Adder_1bit_noCIn_noCOut is
        begin
            Y <= ((A or B) and (not ((A and B))));
        end rtl;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check SPICE
        string spice = module.GetSpice().AsString();
        string expectedSpice =
        """
        .subckt Adder_1bit_noCIn_noCOut A B Y
            .subckt OR2 IN1 IN2 OUT
                VVDD VDD 0 5
                Mpnor1 nor IN1 nor2 nor2 PmosMod
                Mnnor1 nor IN1 0 0 NmosMod
                Mpnor2 nor2 IN2 VDD VDD PmosMod
                Mnnor2 nor IN2 0 0 NmosMod
                Mpnot OUT nor VDD VDD PmosMod
                Mnnot OUT nor 0 0 NmosMod
            .ends OR2
            
            .subckt AND2 IN1 IN2 OUT
                VVDD VDD 0 5
                Mpnand1 nand IN1 VDD VDD PmosMod
                Mnnand1 nand IN1 nand2 nand2 NmosMod
                Mpnand2 nand IN2 VDD VDD PmosMod
                Mnnand2 nand2 IN2 0 0 NmosMod
                Mpnot OUT nand VDD VDD PmosMod
                Mnnot OUT nand 0 0 NmosMod
            .ends AND2
            
            .subckt NOT IN OUT
                VVDD VDD 0 5
                Mp OUT IN VDD VDD PmosMod
                Mn OUT IN 0 0 NmosMod
            .ends NOT
            
            VVDD VDD 0 5
            Rn0_0_0_0x0_res A n0_0_0_0x0_baseout 0.001
            Rn0_0_0_1x0_res B n0_0_0_1x0_baseout 0.001
            Xn0_0_0x0_or n0_0_0_0x0_baseout n0_0_0_1x0_baseout n0_0_0x0_orout OR2
            Rn0_0_1_0_0x0_res A n0_0_1_0_0x0_baseout 0.001
            Rn0_0_1_0_1x0_res B n0_0_1_0_1x0_baseout 0.001
            Xn0_0_1_0x0_and n0_0_1_0_0x0_baseout n0_0_1_0_1x0_baseout n0_0_1_0x0_andout AND2
            Xn0_0_1x0_or n0_0_1_0x0_andout n0_0_1x0_notout NOT
            Xn0_0x0_and n0_0_0x0_orout n0_0_1x0_notout n0_0x0_andout AND2
            Rn0x0_connect n0_0x0_andout Y 0.001
            Rn1x0_floating Y 0 1000000000
        .ends Adder_1bit_noCIn_noCOut

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        XDerivedInstance0 s1 s2 DerivedSignal0 Adder_1bit_noCIn_noCOut
        Rn0_0x0_res DerivedSignal0 n0_0x0_baseout 0.001
        Rn0x0_connect n0_0x0_baseout s3 0.001
        Rn1x0_floating s3 0 1000000000
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check rules
        SimulationRule[] rules = [.. module.GetSimulationRules()];
        SubcircuitReference modRef = new(module, []);
        Assert.AreEqual(2, rules.Length); // One for s3, one for derived signal
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == modRef.GetChildSignalReference(s3)));

        // Run simulation
        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 4e-5
        };
        simulation.StimulusMapping[s1p] = new PulseStimulus(1e-5, 1e-5, 2e-5);
        simulation.StimulusMapping[s2p] = new PulseStimulus(2e-5, 2e-5, 4e-5);

        SubcircuitReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s1));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s2));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s3));
        ISimulationResult[] results = [.. simulation.Simulate()];
        AdderTests.TestResults(results, 1, false, false);
    }

    [TestMethod]
    public void Test1bitWithCarryOut()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Signal s2 = module.GenerateSignal("s2");
        Vector s3 = module.GenerateVector("s3", 2);
        s3.AssignBehavior(s1.Plus(s2, true));
        Port s1p = module.AddNewPort(s1, PortDirection.Input);
        Port s2p = module.AddNewPort(s2, PortDirection.Input);
        module.AddNewPort(s3, PortDirection.Output);

        // Check VHDL
        string vhdl = module.GetVhdl();
        string expectedVhdl =
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity mod1 is
            port (
                s1	: in	std_logic;
                s2	: in	std_logic;
                s3	: out	std_logic_vector(1 downto 0)
            );
        end mod1;

        architecture rtl of mod1 is
            signal DerivedSignal0	: std_logic_vector(1 downto 0)
        component Adder_1bit_noCIn
            port (
                A	: in	std_logic;
                B	: in	std_logic;
                Y	: out	std_logic;
                COut	: out	std_logic
            );
        end component Adder_1bit_noCIn;

        begin
            DerivedInstance0 : Adder_1bit_noCIn
                port map (
                    A => s1,
                    B => s2,
                    Y => DerivedSignal0[0],
                    COut => DerivedSignal0[1]
                );
            
            s3 <= DerivedSignal0;
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
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check SPICE
        string spice = module.GetSpice().AsString();
        string expectedSpice =
        """
        .subckt Adder_1bit_noCIn A B Y COut
            .subckt OR2 IN1 IN2 OUT
                VVDD VDD 0 5
                Mpnor1 nor IN1 nor2 nor2 PmosMod
                Mnnor1 nor IN1 0 0 NmosMod
                Mpnor2 nor2 IN2 VDD VDD PmosMod
                Mnnor2 nor IN2 0 0 NmosMod
                Mpnot OUT nor VDD VDD PmosMod
                Mnnot OUT nor 0 0 NmosMod
            .ends OR2
            
            .subckt AND2 IN1 IN2 OUT
                VVDD VDD 0 5
                Mpnand1 nand IN1 VDD VDD PmosMod
                Mnnand1 nand IN1 nand2 nand2 NmosMod
                Mpnand2 nand IN2 VDD VDD PmosMod
                Mnnand2 nand2 IN2 0 0 NmosMod
                Mpnot OUT nand VDD VDD PmosMod
                Mnnot OUT nand 0 0 NmosMod
            .ends AND2
            
            .subckt NOT IN OUT
                VVDD VDD 0 5
                Mp OUT IN VDD VDD PmosMod
                Mn OUT IN 0 0 NmosMod
            .ends NOT
            
            VVDD VDD 0 5
            Rn0_0_0_0x0_res A n0_0_0_0x0_baseout 0.001
            Rn0_0_0_1x0_res B n0_0_0_1x0_baseout 0.001
            Xn0_0_0x0_or n0_0_0_0x0_baseout n0_0_0_1x0_baseout n0_0_0x0_orout OR2
            Rn0_0_1_0_0x0_res A n0_0_1_0_0x0_baseout 0.001
            Rn0_0_1_0_1x0_res B n0_0_1_0_1x0_baseout 0.001
            Xn0_0_1_0x0_and n0_0_1_0_0x0_baseout n0_0_1_0_1x0_baseout n0_0_1_0x0_andout AND2
            Xn0_0_1x0_or n0_0_1_0x0_andout n0_0_1x0_notout NOT
            Xn0_0x0_and n0_0_0x0_orout n0_0_1x0_notout n0_0x0_andout AND2
            Rn0x0_connect n0_0x0_andout Y 0.001
            Rn1_0_0x0_res A n1_0_0x0_baseout 0.001
            Rn1_0_1x0_res B n1_0_1x0_baseout 0.001
            Xn1_0x0_and n1_0_0x0_baseout n1_0_1x0_baseout n1_0x0_andout AND2
            Rn1x0_connect n1_0x0_andout COut 0.001
            Rn2x0_floating Y 0 1000000000
            Rn3x0_floating COut 0 1000000000
        .ends Adder_1bit_noCIn

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        XDerivedInstance0 s1 s2 DerivedSignal0_0 DerivedSignal0_1 Adder_1bit_noCIn
        Rn0_0x0_res DerivedSignal0_0 n0_0x0_baseout 0.001
        Rn0_0x1_res DerivedSignal0_1 n0_0x1_baseout 0.001
        Rn0x0_connect n0_0x0_baseout s3_0 0.001
        Rn0x1_connect n0_0x1_baseout s3_1 0.001
        Rn1x0_floating s3_0 0 1000000000
        Rn2x1_floating s3_1 0 1000000000
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check rules
        SimulationRule[] rules = [.. module.GetSimulationRules()];
        SubcircuitReference modRef = new(module, []);
        Assert.AreEqual(3, rules.Length); // One for s3, one for derived signal's two bits
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == modRef.GetChildSignalReference(s3)));

        // Run simulation
        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 4e-5
        };
        simulation.StimulusMapping[s1p] = new PulseStimulus(1e-5, 1e-5, 2e-5);
        simulation.StimulusMapping[s2p] = new PulseStimulus(2e-5, 2e-5, 4e-5);

        SubcircuitReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s1));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s2));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s3[0])); // Y
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s3[1])); // COut
        ISimulationResult[] results = [.. simulation.Simulate()];
        AdderTests.TestResults(results, 1, false, true);
    }

    [TestMethod]
    public void Test2bit()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Vector s1 = module.GenerateVector("s1", 2);
        Vector s2 = module.GenerateVector("s2", 2);
        Vector s3 = module.GenerateVector("s3", 2);
        s3.AssignBehavior(s1.Plus(s2));
        Port s1p = module.AddNewPort(s1, PortDirection.Input);
        Port s2p = module.AddNewPort(s2, PortDirection.Input);
        module.AddNewPort(s3, PortDirection.Output);

        // Check VHDL
        string vhdl = module.GetVhdl();
        string expectedVhdl =
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity mod1 is
            port (
                s1	: in	std_logic_vector(1 downto 0);
                s2	: in	std_logic_vector(1 downto 0);
                s3	: out	std_logic_vector(1 downto 0)
            );
        end mod1;

        architecture rtl of mod1 is
            signal DerivedSignal0	: std_logic_vector(1 downto 0)

            component Adder_2bits_noCIn_noCOut
                port (
                    A	: in	std_logic_vector(1 downto 0);
                    B	: in	std_logic_vector(1 downto 0);
                    Y	: out	std_logic_vector(1 downto 0)
                );
            end component Adder_2bits_noCIn_noCOut;
            
        begin
            DerivedInstance0 : Adder_2bits_noCIn_noCOut
                port map (
                    A => s1,
                    B => s2,
                    Y => DerivedSignal0
                );
            
            s3 <= DerivedSignal0;
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
                    A => A[0],
                    B => B[0],
                    Y => Y[0],
                    COut => COut0
                );
            
            Adder1 : Adder_1bit_noCOut
                port map (
                    A => A[1],
                    B => B[1],
                    Y => Y[1],
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
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check SPICE
        string spice = module.GetSpice().AsString();
        string expectedSpice =
        """
        .subckt Adder_2bits_noCIn_noCOut A_0 A_1 B_0 B_1 Y_0 Y_1
            .subckt Adder_1bit_noCIn A B Y COut
                .subckt OR2 IN1 IN2 OUT
                    VVDD VDD 0 5
                    Mpnor1 nor IN1 nor2 nor2 PmosMod
                    Mnnor1 nor IN1 0 0 NmosMod
                    Mpnor2 nor2 IN2 VDD VDD PmosMod
                    Mnnor2 nor IN2 0 0 NmosMod
                    Mpnot OUT nor VDD VDD PmosMod
                    Mnnot OUT nor 0 0 NmosMod
                .ends OR2
                
                .subckt AND2 IN1 IN2 OUT
                    VVDD VDD 0 5
                    Mpnand1 nand IN1 VDD VDD PmosMod
                    Mnnand1 nand IN1 nand2 nand2 NmosMod
                    Mpnand2 nand IN2 VDD VDD PmosMod
                    Mnnand2 nand2 IN2 0 0 NmosMod
                    Mpnot OUT nand VDD VDD PmosMod
                    Mnnot OUT nand 0 0 NmosMod
                .ends AND2
                
                .subckt NOT IN OUT
                    VVDD VDD 0 5
                    Mp OUT IN VDD VDD PmosMod
                    Mn OUT IN 0 0 NmosMod
                .ends NOT
                
                VVDD VDD 0 5
                Rn0_0_0_0x0_res A n0_0_0_0x0_baseout 0.001
                Rn0_0_0_1x0_res B n0_0_0_1x0_baseout 0.001
                Xn0_0_0x0_or n0_0_0_0x0_baseout n0_0_0_1x0_baseout n0_0_0x0_orout OR2
                Rn0_0_1_0_0x0_res A n0_0_1_0_0x0_baseout 0.001
                Rn0_0_1_0_1x0_res B n0_0_1_0_1x0_baseout 0.001
                Xn0_0_1_0x0_and n0_0_1_0_0x0_baseout n0_0_1_0_1x0_baseout n0_0_1_0x0_andout AND2
                Xn0_0_1x0_or n0_0_1_0x0_andout n0_0_1x0_notout NOT
                Xn0_0x0_and n0_0_0x0_orout n0_0_1x0_notout n0_0x0_andout AND2
                Rn0x0_connect n0_0x0_andout Y 0.001
                Rn1_0_0x0_res A n1_0_0x0_baseout 0.001
                Rn1_0_1x0_res B n1_0_1x0_baseout 0.001
                Xn1_0x0_and n1_0_0x0_baseout n1_0_1x0_baseout n1_0x0_andout AND2
                Rn1x0_connect n1_0x0_andout COut 0.001
                Rn2x0_floating Y 0 1000000000
                Rn3x0_floating COut 0 1000000000
            .ends Adder_1bit_noCIn
            
            .subckt Adder_1bit_noCOut A B Y CIn
                .subckt OR2 IN1 IN2 OUT
                    VVDD VDD 0 5
                    Mpnor1 nor IN1 nor2 nor2 PmosMod
                    Mnnor1 nor IN1 0 0 NmosMod
                    Mpnor2 nor2 IN2 VDD VDD PmosMod
                    Mnnor2 nor IN2 0 0 NmosMod
                    Mpnot OUT nor VDD VDD PmosMod
                    Mnnot OUT nor 0 0 NmosMod
                .ends OR2
                
                .subckt AND2 IN1 IN2 OUT
                    VVDD VDD 0 5
                    Mpnand1 nand IN1 VDD VDD PmosMod
                    Mnnand1 nand IN1 nand2 nand2 NmosMod
                    Mpnand2 nand IN2 VDD VDD PmosMod
                    Mnnand2 nand2 IN2 0 0 NmosMod
                    Mpnot OUT nand VDD VDD PmosMod
                    Mnnot OUT nand 0 0 NmosMod
                .ends AND2
                
                .subckt NOT IN OUT
                    VVDD VDD 0 5
                    Mp OUT IN VDD VDD PmosMod
                    Mn OUT IN 0 0 NmosMod
                .ends NOT
                
                VVDD VDD 0 5
                Rn0_0_0_0x0_res A n0_0_0_0x0_baseout 0.001
                Rn0_0_0_1x0_res B n0_0_0_1x0_baseout 0.001
                Xn0_0_0x0_or n0_0_0_0x0_baseout n0_0_0_1x0_baseout n0_0_0x0_orout OR2
                Rn0_0_1_0_0x0_res A n0_0_1_0_0x0_baseout 0.001
                Rn0_0_1_0_1x0_res B n0_0_1_0_1x0_baseout 0.001
                Xn0_0_1_0x0_and n0_0_1_0_0x0_baseout n0_0_1_0_1x0_baseout n0_0_1_0x0_andout AND2
                Xn0_0_1x0_or n0_0_1_0x0_andout n0_0_1x0_notout NOT
                Xn0_0x0_and n0_0_0x0_orout n0_0_1x0_notout n0_0x0_andout AND2
                Rn0x0_connect n0_0x0_andout aXorB 0.001
                Rn1_0_0_0x0_res aXorB n1_0_0_0x0_baseout 0.001
                Rn1_0_0_1x0_res CIn n1_0_0_1x0_baseout 0.001
                Xn1_0_0x0_or n1_0_0_0x0_baseout n1_0_0_1x0_baseout n1_0_0x0_orout OR2
                Rn1_0_1_0_0x0_res aXorB n1_0_1_0_0x0_baseout 0.001
                Rn1_0_1_0_1x0_res CIn n1_0_1_0_1x0_baseout 0.001
                Xn1_0_1_0x0_and n1_0_1_0_0x0_baseout n1_0_1_0_1x0_baseout n1_0_1_0x0_andout AND2
                Xn1_0_1x0_or n1_0_1_0x0_andout n1_0_1x0_notout NOT
                Xn1_0x0_and n1_0_0x0_orout n1_0_1x0_notout n1_0x0_andout AND2
                Rn1x0_connect n1_0x0_andout Y 0.001
                Rn2x0_floating Y 0 1000000000
            .ends Adder_1bit_noCOut
            
            VVDD VDD 0 5
            XAdder0 A_0 B_0 Y_0 COut0 Adder_1bit_noCIn
            XAdder1 A_1 B_1 Y_1 COut0 Adder_1bit_noCOut
            Rn0x0_floating Y_0 0 1000000000
            Rn1x1_floating Y_1 0 1000000000
        .ends Adder_2bits_noCIn_noCOut

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        XDerivedInstance0 s1_0 s1_1 s2_0 s2_1 DerivedSignal0_0 DerivedSignal0_1 Adder_2bits_noCIn_noCOut
        Rn0_0x0_res DerivedSignal0_0 n0_0x0_baseout 0.001
        Rn0_0x1_res DerivedSignal0_1 n0_0x1_baseout 0.001
        Rn0x0_connect n0_0x0_baseout s3_0 0.001
        Rn0x1_connect n0_0x1_baseout s3_1 0.001
        Rn1x0_floating s3_0 0 1000000000
        Rn2x1_floating s3_1 0 1000000000
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check rules
        SimulationRule[] rules = [.. module.GetSimulationRules()];
        SubcircuitReference modRef = new(module, []);
        Assert.AreEqual(5, rules.Length);
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == modRef.GetChildSignalReference(s3)));

        // Run simulation
        RuleBasedSimulation simulation = new(module, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 16e-5
        };
        simulation.StimulusMapping[s1p] = new MultiDimensionalStimulus([new PulseStimulus(1e-5, 1e-5, 2e-5), new PulseStimulus(2e-5, 2e-5, 4e-5)]);
        simulation.StimulusMapping[s2p] = new MultiDimensionalStimulus([new PulseStimulus(4e-5, 4e-5, 8e-5), new PulseStimulus(8e-5, 8e-5, 16e-5)]);

        SubcircuitReference moduleRef = new(module, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s1));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s2));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(s3));
        ISimulationResult[] results = [.. simulation.Simulate()];
        AdderTests.TestResults(results, 2, false, false);
    }

    [TestMethod]
    // Checks functionality of auto-linking a derived signal and then manually linking it
    public void AutoLinkThenManuallyLinkTest()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Signal s2 = module.GenerateSignal("s2");
        Signal s3 = module.GenerateSignal("s3");
        AddedSignal derivedSignal = s1.Plus(s2);
        s3.AssignBehavior(derivedSignal);
        module.AddNewPort(s1, PortDirection.Input);
        module.AddNewPort(s2, PortDirection.Input);
        module.AddNewPort(s3, PortDirection.Output);

        // Load VHDL, triggering compilation
        Assert.IsNull(derivedSignal.LinkedSignal);
        module.GetVhdl();
        // It should have auto-linked a signal
        Assert.IsNotNull(derivedSignal.LinkedSignal);
        INamedSignal autoLinkedSignal = derivedSignal.LinkedSignal;
        // Recompile and confirm it makes a new one
        module.GetVhdl();

        Assert.AreNotEqual(autoLinkedSignal, derivedSignal.LinkedSignal);

        // Manually assign and recompile
        Signal newLinkedSignal = module.GenerateSignal("linked");
        derivedSignal.LinkedSignal = newLinkedSignal;
        module.GetVhdl();
        Assert.AreEqual(newLinkedSignal, derivedSignal.LinkedSignal);

        // Set to null and recompile
        derivedSignal.LinkedSignal = null;
        module.GetVhdl();
        Assert.IsNotNull(derivedSignal.LinkedSignal);
        Assert.AreNotEqual(newLinkedSignal, derivedSignal.LinkedSignal);
        Assert.AreNotEqual(autoLinkedSignal, derivedSignal.LinkedSignal);

        // Check that it causes an error to assign a linked signal with the wrong dimension
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;
        Assert.ThrowsException<Exception>(() => derivedSignal.LinkedSignal = new Vector("v1", module, 2));
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
    }

    [TestMethod]
    // Confirms that a derived signal works when it is inside an instantiation--checks simulation
    public void InstanceUsingDerivedSignalTest()
    {
        // Set up child module--1-bit adder
        Module childMod = new("childMod");
        Signal s1 = childMod.GenerateSignal("s1");
        Signal s2 = childMod.GenerateSignal("s2");
        Signal s3 = childMod.GenerateSignal("s3");
        AddedSignal derivedSignal = s1.Plus(s2);
        s3.AssignBehavior(derivedSignal);
        Port p1 = childMod.AddNewPort(s1, PortDirection.Input);
        Port p2 = childMod.AddNewPort(s2, PortDirection.Input);
        Port p3 = childMod.AddNewPort(s3, PortDirection.Output);

        // Set up top module to use child
        Module mainMod = new("mainMod");
        Signal topS1 = mainMod.GenerateSignal("topS1");
        Signal topS2 = mainMod.GenerateSignal("topS2");
        Signal topS3 = mainMod.GenerateSignal("topS3");
        Port topP1 = mainMod.AddNewPort(topS1, PortDirection.Input);
        Port topP2 = mainMod.AddNewPort(topS2, PortDirection.Input);
        Port topP3 = mainMod.AddNewPort(topS3, PortDirection.Output);
        Instantiation inst = mainMod.AddNewInstantiation(childMod, "Inst");
        inst.PortMapping[p1] = topS1;
        inst.PortMapping[p2] = topS2;
        inst.PortMapping[p3] = topS3;

        // Run simulation
        RuleBasedSimulation simulation = new(mainMod, new DefaultTimeStepGenerator() { MaxTimeStep = 1e-6 })
        {
            Length = 4e-5
        };
        simulation.StimulusMapping[topP1] = new PulseStimulus(1e-5, 1e-5, 2e-5);
        simulation.StimulusMapping[topP2] = new PulseStimulus(2e-5, 2e-5, 4e-5);

        SubcircuitReference moduleRef = new(mainMod, []);
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(topS1));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(topS2));
        simulation.SignalsToMonitor.Add(moduleRef.GetChildSignalReference(topS3));
        ISimulationResult[] results = [.. simulation.Simulate()];
        AdderTests.TestResults(results, 1, false, false);
    }
}