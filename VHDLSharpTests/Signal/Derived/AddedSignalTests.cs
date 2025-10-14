using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class AddedSignalTests
{
    [TestMethod]
    public void BasicTest()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.Inactive;
        Module module = new("mod1");
        Signal s1 = module.GenerateSignal("s1");
        Signal s2 = module.GenerateSignal("s2");
        Signal s3 = module.GenerateSignal("s3");
        s3.AssignBehavior(s1.Plus(s2));
        module.AddNewPort(s1, PortDirection.Input);
        module.AddNewPort(s2, PortDirection.Input);
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
                    signal A	: std_logic => signal s1	: std_logic,
                    signal B	: std_logic => signal s2	: std_logic,
                    signal Y	: std_logic => signal DerivedSignal0	: std_logic
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
    }
}