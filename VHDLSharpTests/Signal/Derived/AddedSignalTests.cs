using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharpTests;

[TestClass]
public class AddedSignalTests
{
    [TestMethod]
    public void BasicTest()
    {
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
    }
}