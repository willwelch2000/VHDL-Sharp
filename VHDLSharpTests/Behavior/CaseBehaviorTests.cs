using VHDLSharp.Behaviors;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class CaseBehaviorTests
{
    [TestMethod]
    public void LiteralTest()
    {
        Module module1 = new("m1");
        Vector selector = new("selector", module1, 2);
        Vector v1 = new("v1", module1, 3);
        Vector v2 = new("v2", module1, 2);

        CaseBehavior behavior =  new(selector);
        Literal l7 = new(7, 3);
        Literal l6 = new(6, 3);
        Literal l3 = new(3, 3);
        Literal l1 = new(1, 3);
        behavior.AddCase(0, l7);
        behavior[1] = new(l6);
        behavior.AddCase(2, l3);
        Assert.IsFalse(behavior.IsComplete());
        behavior.AddCase(3, l1);
        Assert.IsTrue(behavior.IsComplete());
        behavior.AddCase(3, null);
        Assert.IsFalse(behavior.IsComplete());
        behavior.SetDefault(l1);
        Assert.IsTrue(behavior.IsComplete());

        // Basic stuff
        Assert.AreEqual(module1, behavior.ParentModule);
        Assert.AreEqual(new Dimension(3), behavior.Dimension);
        Assert.AreEqual(l7, behavior[0]?.InnerExpression);
        Assert.AreEqual(l6, behavior[1]?.InnerExpression);
        Assert.AreEqual(l3, behavior[2]?.InnerExpression);
        Assert.IsNull(behavior[3]);
        Assert.AreEqual(l1, behavior.DefaultExpression?.InnerExpression);

        // Input signals--only selector
        INamedSignal[] inputs = [.. behavior.NamedInputSignals];
        Assert.AreEqual(1, inputs.Length);
        Assert.AreEqual(selector, inputs[0]);
        Assert.AreEqual(selector, behavior.Selector);
        
        // Compatibility
        Assert.IsTrue(behavior.IsCompatible(v1));
        Assert.IsFalse(behavior.IsCompatible(v2));

        // Check Spice
        string spice = behavior.GetSpice(v1, "0").AsString();
        string expectedSpice = 
        $"""
        {Util.GetNotSubcircuitSpice(false)}
        {Util.GetAndSubcircuitSpice(3, false)}
        {Util.GetOrSubcircuitSpice(4, false)}
        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5

        Rn0_0_0x0_res VDD n0_0_0x0_baseout 0.001
        Rn0_0_0x1_res VDD n0_0_0x1_baseout 0.001
        Rn0_0_0x2_res VDD n0_0_0x2_baseout 0.001

        Rn0_0x0_connect n0_0_0x0_baseout n0x0_case0_0 0.001
        Rn0_0x1_connect n0_0_0x1_baseout n0x0_case0_1 0.001
        Rn0_0x2_connect n0_0_0x2_baseout n0x0_case0_2 0.001

        Rn0_1_0x0_res 0 n0_1_0x0_baseout 0.001
        Rn0_1_0x1_res VDD n0_1_0x1_baseout 0.001
        Rn0_1_0x2_res VDD n0_1_0x2_baseout 0.001

        Rn0_1x0_connect n0_1_0x0_baseout n0x0_case1_0 0.001
        Rn0_1x1_connect n0_1_0x1_baseout n0x0_case1_1 0.001
        Rn0_1x2_connect n0_1_0x2_baseout n0x0_case1_2 0.001

        Rn0_2_0x0_res VDD n0_2_0x0_baseout 0.001
        Rn0_2_0x1_res VDD n0_2_0x1_baseout 0.001
        Rn0_2_0x2_res 0 n0_2_0x2_baseout 0.001

        Rn0_2x0_connect n0_2_0x0_baseout n0x0_case2_0 0.001
        Rn0_2x1_connect n0_2_0x1_baseout n0x0_case2_1 0.001
        Rn0_2x2_connect n0_2_0x2_baseout n0x0_case2_2 0.001

        Rn0_3_0x0_res VDD n0_3_0x0_baseout 0.001
        Rn0_3_0x1_res 0 n0_3_0x1_baseout 0.001
        Rn0_3_0x2_res 0 n0_3_0x2_baseout 0.001

        Rn0_3x0_connect n0_3_0x0_baseout n0x0_case3_0 0.001
        Rn0_3x1_connect n0_3_0x1_baseout n0x0_case3_1 0.001
        Rn0_3x2_connect n0_3_0x2_baseout n0x0_case3_2 0.001

        Rn0_4_0_0_0_0x0_res selector_0 n0_4_0_0_0_0x0_baseout 0.001
        Xn0_4_0_0_0x0_or n0_4_0_0_0_0x0_baseout n0_4_0_0_0x0_notout NOT

        Rn0_4_0_0_1_0x0_res selector_1 n0_4_0_0_1_0x0_baseout 0.001
        Xn0_4_0_0_1x0_or n0_4_0_0_1_0x0_baseout n0_4_0_0_1x0_notout NOT

        Rn0_4_0_0_2x0_res n0x0_case0_0 n0_4_0_0_2x0_baseout 0.001
        Xn0_4_0_0x0_and n0_4_0_0_0x0_notout n0_4_0_0_1x0_notout n0_4_0_0_2x0_baseout n0_4_0_0x0_andout AND3

        Rn0_4_0_1_0x0_res selector_0 n0_4_0_1_0x0_baseout 0.001
        Rn0_4_0_1_1_0x0_res selector_1 n0_4_0_1_1_0x0_baseout 0.001
        Xn0_4_0_1_1x0_or n0_4_0_1_1_0x0_baseout n0_4_0_1_1x0_notout NOT

        Rn0_4_0_1_2x0_res n0x0_case1_0 n0_4_0_1_2x0_baseout 0.001
        Xn0_4_0_1x0_and n0_4_0_1_0x0_baseout n0_4_0_1_1x0_notout n0_4_0_1_2x0_baseout n0_4_0_1x0_andout AND3

        Rn0_4_0_2_0_0x0_res selector_0 n0_4_0_2_0_0x0_baseout 0.001
        Xn0_4_0_2_0x0_or n0_4_0_2_0_0x0_baseout n0_4_0_2_0x0_notout NOT

        Rn0_4_0_2_1x0_res selector_1 n0_4_0_2_1x0_baseout 0.001
        Rn0_4_0_2_2x0_res n0x0_case2_0 n0_4_0_2_2x0_baseout 0.001
        Xn0_4_0_2x0_and n0_4_0_2_0x0_notout n0_4_0_2_1x0_baseout n0_4_0_2_2x0_baseout n0_4_0_2x0_andout AND3

        Rn0_4_0_3_0x0_res selector_0 n0_4_0_3_0x0_baseout 0.001
        Rn0_4_0_3_1x0_res selector_1 n0_4_0_3_1x0_baseout 0.001
        Rn0_4_0_3_2x0_res n0x0_case3_0 n0_4_0_3_2x0_baseout 0.001
        Xn0_4_0_3x0_and n0_4_0_3_0x0_baseout n0_4_0_3_1x0_baseout n0_4_0_3_2x0_baseout n0_4_0_3x0_andout AND3
        Xn0_4_0x0_or n0_4_0_0x0_andout n0_4_0_1x0_andout n0_4_0_2x0_andout n0_4_0_3x0_andout n0_4_0x0_orout OR4

        Rn0_4x0_connect n0_4_0x0_orout v1_0 0.001
        Rn0_5_0_0_0_0x0_res selector_0 n0_5_0_0_0_0x0_baseout 0.001
        Xn0_5_0_0_0x0_or n0_5_0_0_0_0x0_baseout n0_5_0_0_0x0_notout NOT

        Rn0_5_0_0_1_0x0_res selector_1 n0_5_0_0_1_0x0_baseout 0.001
        Xn0_5_0_0_1x0_or n0_5_0_0_1_0x0_baseout n0_5_0_0_1x0_notout NOT

        Rn0_5_0_0_2x0_res n0x0_case0_1 n0_5_0_0_2x0_baseout 0.001
        Xn0_5_0_0x0_and n0_5_0_0_0x0_notout n0_5_0_0_1x0_notout n0_5_0_0_2x0_baseout n0_5_0_0x0_andout AND3

        Rn0_5_0_1_0x0_res selector_0 n0_5_0_1_0x0_baseout 0.001
        Rn0_5_0_1_1_0x0_res selector_1 n0_5_0_1_1_0x0_baseout 0.001
        Xn0_5_0_1_1x0_or n0_5_0_1_1_0x0_baseout n0_5_0_1_1x0_notout NOT

        Rn0_5_0_1_2x0_res n0x0_case1_1 n0_5_0_1_2x0_baseout 0.001
        Xn0_5_0_1x0_and n0_5_0_1_0x0_baseout n0_5_0_1_1x0_notout n0_5_0_1_2x0_baseout n0_5_0_1x0_andout AND3

        Rn0_5_0_2_0_0x0_res selector_0 n0_5_0_2_0_0x0_baseout 0.001
        Xn0_5_0_2_0x0_or n0_5_0_2_0_0x0_baseout n0_5_0_2_0x0_notout NOT

        Rn0_5_0_2_1x0_res selector_1 n0_5_0_2_1x0_baseout 0.001
        Rn0_5_0_2_2x0_res n0x0_case2_1 n0_5_0_2_2x0_baseout 0.001
        Xn0_5_0_2x0_and n0_5_0_2_0x0_notout n0_5_0_2_1x0_baseout n0_5_0_2_2x0_baseout n0_5_0_2x0_andout AND3

        Rn0_5_0_3_0x0_res selector_0 n0_5_0_3_0x0_baseout 0.001
        Rn0_5_0_3_1x0_res selector_1 n0_5_0_3_1x0_baseout 0.001
        Rn0_5_0_3_2x0_res n0x0_case3_1 n0_5_0_3_2x0_baseout 0.001

        Xn0_5_0_3x0_and n0_5_0_3_0x0_baseout n0_5_0_3_1x0_baseout n0_5_0_3_2x0_baseout n0_5_0_3x0_andout AND3
        Xn0_5_0x0_or n0_5_0_0x0_andout n0_5_0_1x0_andout n0_5_0_2x0_andout n0_5_0_3x0_andout n0_5_0x0_orout OR4

        Rn0_5x0_connect n0_5_0x0_orout v1_1 0.001
        Rn0_6_0_0_0_0x0_res selector_0 n0_6_0_0_0_0x0_baseout 0.001
        Xn0_6_0_0_0x0_or n0_6_0_0_0_0x0_baseout n0_6_0_0_0x0_notout NOT

        Rn0_6_0_0_1_0x0_res selector_1 n0_6_0_0_1_0x0_baseout 0.001
        Xn0_6_0_0_1x0_or n0_6_0_0_1_0x0_baseout n0_6_0_0_1x0_notout NOT

        Rn0_6_0_0_2x0_res n0x0_case0_2 n0_6_0_0_2x0_baseout 0.001
        Xn0_6_0_0x0_and n0_6_0_0_0x0_notout n0_6_0_0_1x0_notout n0_6_0_0_2x0_baseout n0_6_0_0x0_andout AND3

        Rn0_6_0_1_0x0_res selector_0 n0_6_0_1_0x0_baseout 0.001
        Rn0_6_0_1_1_0x0_res selector_1 n0_6_0_1_1_0x0_baseout 0.001
        Xn0_6_0_1_1x0_or n0_6_0_1_1_0x0_baseout n0_6_0_1_1x0_notout NOT

        Rn0_6_0_1_2x0_res n0x0_case1_2 n0_6_0_1_2x0_baseout 0.001
        Xn0_6_0_1x0_and n0_6_0_1_0x0_baseout n0_6_0_1_1x0_notout n0_6_0_1_2x0_baseout n0_6_0_1x0_andout AND3

        Rn0_6_0_2_0_0x0_res selector_0 n0_6_0_2_0_0x0_baseout 0.001
        Xn0_6_0_2_0x0_or n0_6_0_2_0_0x0_baseout n0_6_0_2_0x0_notout NOT

        Rn0_6_0_2_1x0_res selector_1 n0_6_0_2_1x0_baseout 0.001
        Rn0_6_0_2_2x0_res n0x0_case2_2 n0_6_0_2_2x0_baseout 0.001
        Xn0_6_0_2x0_and n0_6_0_2_0x0_notout n0_6_0_2_1x0_baseout n0_6_0_2_2x0_baseout n0_6_0_2x0_andout AND3

        Rn0_6_0_3_0x0_res selector_0 n0_6_0_3_0x0_baseout 0.001
        Rn0_6_0_3_1x0_res selector_1 n0_6_0_3_1x0_baseout 0.001
        Rn0_6_0_3_2x0_res n0x0_case3_2 n0_6_0_3_2x0_baseout 0.001

        Xn0_6_0_3x0_and n0_6_0_3_0x0_baseout n0_6_0_3_1x0_baseout n0_6_0_3_2x0_baseout n0_6_0_3x0_andout AND3
        Xn0_6_0x0_or n0_6_0_0x0_andout n0_6_0_1x0_andout n0_6_0_2x0_andout n0_6_0_3x0_andout n0_6_0x0_orout OR4
        Rn0_6x0_connect n0_6_0x0_orout v1_2 0.001
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(spice, expectedSpice));

        // Check VHDL
        string vhdl = behavior.GetVhdlStatement(v1);
        string expectedVhdl = 
        """
        process(selector) is
        begin
            case selector is
                when "00" =>
                    v1 <= "111";
                when "01" =>
                    v1 <= "110";
                when "10" =>
                    v1 <= "011";
                when others =>
                    v1 <= "001";
            end case;
        end process;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(vhdl, expectedVhdl));

        // Check simulation rule and its output values
        SubcircuitReference subcircuitRef = new(module1, []);
        SignalReference v1Ref = new(subcircuitRef, v1);
        SimulationRule simRule = behavior.GetSimulationRule(v1Ref);
        Assert.AreEqual(v1Ref, simRule.OutputSignal);
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        SignalReference selectorRef = new(subcircuitRef, selector);
        for (int i = 0; i < 4; i++)
        {
            RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint(new()
            {
                {selectorRef, [i]}
            }, [0, 1], 1);
            int value = simRule.OutputValueCalculation(state);
            int expectedValue = i switch
            {
                0 => 7,
                1 => 6,
                2 => 3,
                _ => 1,
            };
            Assert.AreEqual(expectedValue, value);
        }
    }
}