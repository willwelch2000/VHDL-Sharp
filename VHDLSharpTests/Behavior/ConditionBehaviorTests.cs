using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class ConditionBehaviorTests
{
    [TestMethod]
    public void BasicTest()
    {
        Module module = new("m1");
        Vector input = new("IN", module, 3);
        Literal comparison = new(3, 3);
        Vector output = new("OUT", module, 2);

        ConditionBehavior behavior = new()
        {
            DefaultBehavior = new ValueBehavior(0)
        };
        behavior.Add(input.EqualTo(comparison), new ValueBehavior(1));
        behavior.ConditionMappings.Add((input.GreaterThan(comparison), new ValueBehavior(2)));
        output.AssignBehavior(behavior);

        // Remove and reapply to check
        behavior.Remove(input.GreaterThan(comparison));
        Assert.AreEqual(1, behavior.ConditionMappings.Count);
        behavior[input.GreaterThan(comparison)] = new ValueBehavior(1);
        Assert.AreEqual(2, behavior.ConditionMappings.Count);
        behavior[input.GreaterThan(comparison)] = new ValueBehavior(2);
        Assert.AreEqual(2, behavior.ConditionMappings.Count);

        // Check VHDL
        string vhdl = behavior.GetVhdlStatement(output);
        string expectedVhdl =
        """
        if (IN = "011") then
            OUT <= "01";
        elsif (unsigned(IN) > unsigned("011")) then
            OUT <= "10";
        else
            OUT <= "00";
        end if
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check Spice
        string spice = behavior.GetSpice(output, "0").AsString();
        string expectedSpice =
        $"""
        .subckt XNOR2 IN1 IN2 OUT
            {Util.GetNandSubcircuitSpice(2, false)}
            
            VVDD VDD 0 5
            Xnand1 IN1 IN2 nand1out NAND2
            Xor IN1 IN2 orout OR2
            Xnand2 nand1out orout OUT NAND2
        .ends XNOR2
        {Util.GetAndSubcircuitSpice(3, false)}
        {Util.GetNotSubcircuitSpice(false)}
        {Util.GetAndSubcircuitSpice(2, false)}
        {Util.GetOrSubcircuitSpice(2, false)}

        .subckt MUX1 SEL1 IN1 IN2 OUT
            {Util.GetNandSubcircuitSpice(2, false)}
            
            VVDD VDD 0 5
            Xnot1 SEL1 SEL1_not NOT
            XnandIn1 IN1 SEL1_not int1 NAND2
            XnandIn2 IN2 SEL1 int2 NAND2
            Xfinal int1 int2 OUT NAND2
        .ends MUX1

        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5
        Vn0_0_0x0_value n0x0_inner0_0 0 5
        Vn0_0_0x1_value n0x0_inner0_1 0 0
        Xn0_0_1x0_equalityXnorInt IN_0 VDD n0_0_1x0_equalityInt XNOR2
        Xn0_0_1x1_equalityXnorInt IN_1 VDD n0_0_1x1_equalityInt XNOR2
        Xn0_0_1x2_equalityXnorInt IN_2 0 n0_0_1x2_equalityInt XNOR2
        Xn0_0_1x0_equalityAndFinal n0_0_1x0_equalityInt n0_0_1x1_equalityInt n0_0_1x2_equalityInt n0x0_condition0 AND3
        Vn0_1_0x0_value n0x0_inner1_0 0 0
        Vn0_1_0x1_value n0x0_inner1_1 0 5
        Rn0_1_1_0_0_0x0_res IN_2 n0_1_1_0_0_0x0_baseout 0.001
        Rn0_1_1_0_0_1_0x0_res 0 n0_1_1_0_0_1_0x0_baseout 0.001
        Xn0_1_1_0_0_1x0_or n0_1_1_0_0_1_0x0_baseout n0_1_1_0_0_1x0_notout NOT
        Xn0_1_1_0_0x0_and n0_1_1_0_0_0x0_baseout n0_1_1_0_0_1x0_notout n0_1_1_0_0x0_andout AND2
        Rn0_1_1_0_1_0_0_0x0_res IN_2 n0_1_1_0_1_0_0_0x0_baseout 0.001
        Rn0_1_1_0_1_0_0_1x0_res 0 n0_1_1_0_1_0_0_1x0_baseout 0.001
        Xn0_1_1_0_1_0_0x0_and n0_1_1_0_1_0_0_0x0_baseout n0_1_1_0_1_0_0_1x0_baseout n0_1_1_0_1_0_0x0_andout AND2
        Rn0_1_1_0_1_0_1_0_0x0_res IN_2 n0_1_1_0_1_0_1_0_0x0_baseout 0.001
        Xn0_1_1_0_1_0_1_0x0_or n0_1_1_0_1_0_1_0_0x0_baseout n0_1_1_0_1_0_1_0x0_notout NOT
        Rn0_1_1_0_1_0_1_1_0x0_res 0 n0_1_1_0_1_0_1_1_0x0_baseout 0.001
        Xn0_1_1_0_1_0_1_1x0_or n0_1_1_0_1_0_1_1_0x0_baseout n0_1_1_0_1_0_1_1x0_notout NOT
        Xn0_1_1_0_1_0_1x0_and n0_1_1_0_1_0_1_0x0_notout n0_1_1_0_1_0_1_1x0_notout n0_1_1_0_1_0_1x0_andout AND2
        Xn0_1_1_0_1_0x0_or n0_1_1_0_1_0_0x0_andout n0_1_1_0_1_0_1x0_andout n0_1_1_0_1_0x0_orout OR2
        Rn0_1_1_0_1_1_0_0x0_res IN_1 n0_1_1_0_1_1_0_0x0_baseout 0.001
        Rn0_1_1_0_1_1_0_1_0x0_res VDD n0_1_1_0_1_1_0_1_0x0_baseout 0.001
        Xn0_1_1_0_1_1_0_1x0_or n0_1_1_0_1_1_0_1_0x0_baseout n0_1_1_0_1_1_0_1x0_notout NOT
        Xn0_1_1_0_1_1_0x0_and n0_1_1_0_1_1_0_0x0_baseout n0_1_1_0_1_1_0_1x0_notout n0_1_1_0_1_1_0x0_andout AND2
        Rn0_1_1_0_1_1_1_0_0_0x0_res IN_1 n0_1_1_0_1_1_1_0_0_0x0_baseout 0.001
        Rn0_1_1_0_1_1_1_0_0_1x0_res VDD n0_1_1_0_1_1_1_0_0_1x0_baseout 0.001
        Xn0_1_1_0_1_1_1_0_0x0_and n0_1_1_0_1_1_1_0_0_0x0_baseout n0_1_1_0_1_1_1_0_0_1x0_baseout n0_1_1_0_1_1_1_0_0x0_andout AND2
        Rn0_1_1_0_1_1_1_0_1_0_0x0_res IN_1 n0_1_1_0_1_1_1_0_1_0_0x0_baseout 0.001
        Xn0_1_1_0_1_1_1_0_1_0x0_or n0_1_1_0_1_1_1_0_1_0_0x0_baseout n0_1_1_0_1_1_1_0_1_0x0_notout NOT
        Rn0_1_1_0_1_1_1_0_1_1_0x0_res VDD n0_1_1_0_1_1_1_0_1_1_0x0_baseout 0.001
        Xn0_1_1_0_1_1_1_0_1_1x0_or n0_1_1_0_1_1_1_0_1_1_0x0_baseout n0_1_1_0_1_1_1_0_1_1x0_notout NOT
        Xn0_1_1_0_1_1_1_0_1x0_and n0_1_1_0_1_1_1_0_1_0x0_notout n0_1_1_0_1_1_1_0_1_1x0_notout n0_1_1_0_1_1_1_0_1x0_andout AND2
        Xn0_1_1_0_1_1_1_0x0_or n0_1_1_0_1_1_1_0_0x0_andout n0_1_1_0_1_1_1_0_1x0_andout n0_1_1_0_1_1_1_0x0_orout OR2
        Rn0_1_1_0_1_1_1_1_0x0_res IN_0 n0_1_1_0_1_1_1_1_0x0_baseout 0.001
        Rn0_1_1_0_1_1_1_1_1_0x0_res VDD n0_1_1_0_1_1_1_1_1_0x0_baseout 0.001
        Xn0_1_1_0_1_1_1_1_1x0_or n0_1_1_0_1_1_1_1_1_0x0_baseout n0_1_1_0_1_1_1_1_1x0_notout NOT
        Xn0_1_1_0_1_1_1_1x0_and n0_1_1_0_1_1_1_1_0x0_baseout n0_1_1_0_1_1_1_1_1x0_notout n0_1_1_0_1_1_1_1x0_andout AND2
        Xn0_1_1_0_1_1_1x0_and n0_1_1_0_1_1_1_0x0_orout n0_1_1_0_1_1_1_1x0_andout n0_1_1_0_1_1_1x0_andout AND2
        Xn0_1_1_0_1_1x0_or n0_1_1_0_1_1_0x0_andout n0_1_1_0_1_1_1x0_andout n0_1_1_0_1_1x0_orout OR2
        Xn0_1_1_0_1x0_and n0_1_1_0_1_0x0_orout n0_1_1_0_1_1x0_orout n0_1_1_0_1x0_andout AND2
        Xn0_1_1_0x0_or n0_1_1_0_0x0_andout n0_1_1_0_1x0_andout n0_1_1_0x0_orout OR2
        Rn0_1_1x0_connect n0_1_1_0x0_orout n0x0_condition1 0.001
        Vn0_2x0_value n0x0_default_0 0 0
        Vn0_2x1_value n0x0_default_1 0 0
        Xn0x0_MUX0 n0x0_condition0 n0x0_MUXOut0_0 n0x0_inner0_0 OUT_0 MUX1
        Xn0x1_MUX0 n0x0_condition0 n0x0_MUXOut0_1 n0x0_inner0_1 OUT_1 MUX1
        Xn0x0_MUX1 n0x0_condition1 n0x0_default_0 n0x0_inner1_0 OUT_0 MUX1
        Xn0x1_MUX1 n0x0_condition1 n0x0_default_1 n0x0_inner1_1 OUT_1 MUX1
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check simulation rule and its output values
        SubmoduleReference submoduleRef = new(module, []);
        SignalReference outputRef = new(submoduleRef, output);
        SignalReference inputRef = new(submoduleRef, input);
        SimulationRule simRule = behavior.GetSimulationRule(outputRef);
        for (int i = 0; i < 8; i++)
        {
            RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint(new()
            {
                {inputRef, [i]}
            }, [0, 1], 1);
            int value = simRule.OutputValueCalculation(state);
            int expectedValue = i switch
            {
                < 3 => 0,
                3 => 1,
                _ => 2,
            };
            Assert.AreEqual(expectedValue, value);
        }
    }
}