using VHDLSharp.Behaviors;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

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
        string spice = behavior.GetSpice(v1, "0");
        string expectedSpice = 
        """
        Rn0_0x0_res VDD n0x0_case0_0 1m

        Rn0_0x1_res VDD n0x0_case0_1 1m

        Rn0_0x2_res VDD n0x0_case0_2 1m

        Rn0_1x0_res 0 n0x0_case1_0 1m

        Rn0_1x1_res VDD n0x0_case1_1 1m

        Rn0_1x2_res VDD n0x0_case1_2 1m

        Rn0_2x0_res VDD n0x0_case2_0 1m

        Rn0_2x1_res VDD n0x0_case2_1 1m

        Rn0_2x2_res 0 n0x0_case2_2 1m

        Rn0_3x0_res VDD n0x0_case3_0 1m

        Rn0_3x1_res 0 n0x0_case3_1 1m

        Rn0_3x2_res 0 n0x0_case3_2 1m

        Rn0_4_0_0_0x0_res selector_0 n0_4_0_0_0x0_baseout 1m
        Mn0_4_0_0x0_p n0_4_0_0x0_notout n0_4_0_0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_0_0x0_n n0_4_0_0x0_notout n0_4_0_0_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_4_0_1_0x0_res selector_1 n0_4_0_1_0x0_baseout 1m
        Mn0_4_0_1x0_p n0_4_0_1x0_notout n0_4_0_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_0_1x0_n n0_4_0_1x0_notout n0_4_0_1_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_4_0_2x0_res n0x0_case0_0 n0_4_0_2x0_baseout 1m
        Mn0_4_0x0_pnand0 n0_4_0x0_nandout n0_4_0_0x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_4_0x0_nnand0 n0_4_0x0_nandout n0_4_0_0x0_notout n0_4_0x0_nand1 n0_4_0x0_nand1 NmosMod W=100u L=1u
        Mn0_4_0x0_pnand1 n0_4_0x0_nandout n0_4_0_1x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_4_0x0_nnand1 n0_4_0x0_nand1 n0_4_0_1x0_notout n0_4_0x0_nand2 n0_4_0x0_nand2 NmosMod W=100u L=1u
        Mn0_4_0x0_pnand2 n0_4_0x0_nandout n0_4_0_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_0x0_nnand2 n0_4_0x0_nand2 n0_4_0_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_4_0x0_pnot n0_4_0x0_andout n0_4_0x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_4_0x0_nnot n0_4_0x0_andout n0_4_0x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_4_1_0x0_res selector_0 n0_4_1_0x0_baseout 1m
        Rn0_4_1_1_0x0_res selector_1 n0_4_1_1_0x0_baseout 1m
        Mn0_4_1_1x0_p n0_4_1_1x0_notout n0_4_1_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_1_1x0_n n0_4_1_1x0_notout n0_4_1_1_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_4_1_2x0_res n0x0_case1_0 n0_4_1_2x0_baseout 1m
        Mn0_4_1x0_pnand0 n0_4_1x0_nandout n0_4_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_1x0_nnand0 n0_4_1x0_nandout n0_4_1_0x0_baseout n0_4_1x0_nand1 n0_4_1x0_nand1 NmosMod W=100u L=1u
        Mn0_4_1x0_pnand1 n0_4_1x0_nandout n0_4_1_1x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_4_1x0_nnand1 n0_4_1x0_nand1 n0_4_1_1x0_notout n0_4_1x0_nand2 n0_4_1x0_nand2 NmosMod W=100u L=1u
        Mn0_4_1x0_pnand2 n0_4_1x0_nandout n0_4_1_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_1x0_nnand2 n0_4_1x0_nand2 n0_4_1_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_4_1x0_pnot n0_4_1x0_andout n0_4_1x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_4_1x0_nnot n0_4_1x0_andout n0_4_1x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_4_2_0_0x0_res selector_0 n0_4_2_0_0x0_baseout 1m
        Mn0_4_2_0x0_p n0_4_2_0x0_notout n0_4_2_0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_2_0x0_n n0_4_2_0x0_notout n0_4_2_0_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_4_2_1x0_res selector_1 n0_4_2_1x0_baseout 1m
        Rn0_4_2_2x0_res n0x0_case2_0 n0_4_2_2x0_baseout 1m
        Mn0_4_2x0_pnand0 n0_4_2x0_nandout n0_4_2_0x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_4_2x0_nnand0 n0_4_2x0_nandout n0_4_2_0x0_notout n0_4_2x0_nand1 n0_4_2x0_nand1 NmosMod W=100u L=1u
        Mn0_4_2x0_pnand1 n0_4_2x0_nandout n0_4_2_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_2x0_nnand1 n0_4_2x0_nand1 n0_4_2_1x0_baseout n0_4_2x0_nand2 n0_4_2x0_nand2 NmosMod W=100u L=1u
        Mn0_4_2x0_pnand2 n0_4_2x0_nandout n0_4_2_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_2x0_nnand2 n0_4_2x0_nand2 n0_4_2_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_4_2x0_pnot n0_4_2x0_andout n0_4_2x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_4_2x0_nnot n0_4_2x0_andout n0_4_2x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_4_3_0x0_res selector_0 n0_4_3_0x0_baseout 1m
        Rn0_4_3_1x0_res selector_1 n0_4_3_1x0_baseout 1m
        Rn0_4_3_2x0_res n0x0_case3_0 n0_4_3_2x0_baseout 1m
        Mn0_4_3x0_pnand0 n0_4_3x0_nandout n0_4_3_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_3x0_nnand0 n0_4_3x0_nandout n0_4_3_0x0_baseout n0_4_3x0_nand1 n0_4_3x0_nand1 NmosMod W=100u L=1u
        Mn0_4_3x0_pnand1 n0_4_3x0_nandout n0_4_3_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_3x0_nnand1 n0_4_3x0_nand1 n0_4_3_1x0_baseout n0_4_3x0_nand2 n0_4_3x0_nand2 NmosMod W=100u L=1u
        Mn0_4_3x0_pnand2 n0_4_3x0_nandout n0_4_3_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_4_3x0_nnand2 n0_4_3x0_nand2 n0_4_3_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_4_3x0_pnot n0_4_3x0_andout n0_4_3x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_4_3x0_nnot n0_4_3x0_andout n0_4_3x0_nandout 0 0 NmosMod W=100u L=1u

        Mn0_4x0_pnor0 n0_4x0_norout n0_4_0x0_andout n0_4x0_nor1 n0_4x0_nor1 PmosMod W=100u L=1u
        Mn0_4x0_nnor0 n0_4x0_norout n0_4_0x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_4x0_pnor1 n0_4x0_nor1 n0_4_1x0_andout n0_4x0_nor2 n0_4x0_nor2 PmosMod W=100u L=1u
        Mn0_4x0_nnor1 n0_4x0_norout n0_4_1x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_4x0_pnor2 n0_4x0_nor2 n0_4_2x0_andout n0_4x0_nor3 n0_4x0_nor3 PmosMod W=100u L=1u
        Mn0_4x0_nnor2 n0_4x0_norout n0_4_2x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_4x0_pnor3 n0_4x0_nor3 n0_4_3x0_andout VDD VDD PmosMod W=100u L=1u
        Mn0_4x0_nnor3 n0_4x0_norout n0_4_3x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_4x0_pnot v1_0 n0_4x0_norout VDD VDD PmosMod W=100u L=1u
        Mn0_4x0_nnot v1_0 n0_4x0_norout 0 0 NmosMod W=100u L=1u


        Rn0_5_0_0_0x0_res selector_0 n0_5_0_0_0x0_baseout 1m
        Mn0_5_0_0x0_p n0_5_0_0x0_notout n0_5_0_0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_0_0x0_n n0_5_0_0x0_notout n0_5_0_0_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_5_0_1_0x0_res selector_1 n0_5_0_1_0x0_baseout 1m
        Mn0_5_0_1x0_p n0_5_0_1x0_notout n0_5_0_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_0_1x0_n n0_5_0_1x0_notout n0_5_0_1_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_5_0_2x0_res n0x0_case0_1 n0_5_0_2x0_baseout 1m
        Mn0_5_0x0_pnand0 n0_5_0x0_nandout n0_5_0_0x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_5_0x0_nnand0 n0_5_0x0_nandout n0_5_0_0x0_notout n0_5_0x0_nand1 n0_5_0x0_nand1 NmosMod W=100u L=1u
        Mn0_5_0x0_pnand1 n0_5_0x0_nandout n0_5_0_1x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_5_0x0_nnand1 n0_5_0x0_nand1 n0_5_0_1x0_notout n0_5_0x0_nand2 n0_5_0x0_nand2 NmosMod W=100u L=1u
        Mn0_5_0x0_pnand2 n0_5_0x0_nandout n0_5_0_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_0x0_nnand2 n0_5_0x0_nand2 n0_5_0_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_5_0x0_pnot n0_5_0x0_andout n0_5_0x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_5_0x0_nnot n0_5_0x0_andout n0_5_0x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_5_1_0x0_res selector_0 n0_5_1_0x0_baseout 1m
        Rn0_5_1_1_0x0_res selector_1 n0_5_1_1_0x0_baseout 1m
        Mn0_5_1_1x0_p n0_5_1_1x0_notout n0_5_1_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_1_1x0_n n0_5_1_1x0_notout n0_5_1_1_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_5_1_2x0_res n0x0_case1_1 n0_5_1_2x0_baseout 1m
        Mn0_5_1x0_pnand0 n0_5_1x0_nandout n0_5_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_1x0_nnand0 n0_5_1x0_nandout n0_5_1_0x0_baseout n0_5_1x0_nand1 n0_5_1x0_nand1 NmosMod W=100u L=1u
        Mn0_5_1x0_pnand1 n0_5_1x0_nandout n0_5_1_1x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_5_1x0_nnand1 n0_5_1x0_nand1 n0_5_1_1x0_notout n0_5_1x0_nand2 n0_5_1x0_nand2 NmosMod W=100u L=1u
        Mn0_5_1x0_pnand2 n0_5_1x0_nandout n0_5_1_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_1x0_nnand2 n0_5_1x0_nand2 n0_5_1_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_5_1x0_pnot n0_5_1x0_andout n0_5_1x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_5_1x0_nnot n0_5_1x0_andout n0_5_1x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_5_2_0_0x0_res selector_0 n0_5_2_0_0x0_baseout 1m
        Mn0_5_2_0x0_p n0_5_2_0x0_notout n0_5_2_0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_2_0x0_n n0_5_2_0x0_notout n0_5_2_0_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_5_2_1x0_res selector_1 n0_5_2_1x0_baseout 1m
        Rn0_5_2_2x0_res n0x0_case2_1 n0_5_2_2x0_baseout 1m
        Mn0_5_2x0_pnand0 n0_5_2x0_nandout n0_5_2_0x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_5_2x0_nnand0 n0_5_2x0_nandout n0_5_2_0x0_notout n0_5_2x0_nand1 n0_5_2x0_nand1 NmosMod W=100u L=1u
        Mn0_5_2x0_pnand1 n0_5_2x0_nandout n0_5_2_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_2x0_nnand1 n0_5_2x0_nand1 n0_5_2_1x0_baseout n0_5_2x0_nand2 n0_5_2x0_nand2 NmosMod W=100u L=1u
        Mn0_5_2x0_pnand2 n0_5_2x0_nandout n0_5_2_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_2x0_nnand2 n0_5_2x0_nand2 n0_5_2_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_5_2x0_pnot n0_5_2x0_andout n0_5_2x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_5_2x0_nnot n0_5_2x0_andout n0_5_2x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_5_3_0x0_res selector_0 n0_5_3_0x0_baseout 1m
        Rn0_5_3_1x0_res selector_1 n0_5_3_1x0_baseout 1m
        Rn0_5_3_2x0_res n0x0_case3_1 n0_5_3_2x0_baseout 1m
        Mn0_5_3x0_pnand0 n0_5_3x0_nandout n0_5_3_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_3x0_nnand0 n0_5_3x0_nandout n0_5_3_0x0_baseout n0_5_3x0_nand1 n0_5_3x0_nand1 NmosMod W=100u L=1u
        Mn0_5_3x0_pnand1 n0_5_3x0_nandout n0_5_3_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_3x0_nnand1 n0_5_3x0_nand1 n0_5_3_1x0_baseout n0_5_3x0_nand2 n0_5_3x0_nand2 NmosMod W=100u L=1u
        Mn0_5_3x0_pnand2 n0_5_3x0_nandout n0_5_3_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_5_3x0_nnand2 n0_5_3x0_nand2 n0_5_3_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_5_3x0_pnot n0_5_3x0_andout n0_5_3x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_5_3x0_nnot n0_5_3x0_andout n0_5_3x0_nandout 0 0 NmosMod W=100u L=1u

        Mn0_5x0_pnor0 n0_5x0_norout n0_5_0x0_andout n0_5x0_nor1 n0_5x0_nor1 PmosMod W=100u L=1u
        Mn0_5x0_nnor0 n0_5x0_norout n0_5_0x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_5x0_pnor1 n0_5x0_nor1 n0_5_1x0_andout n0_5x0_nor2 n0_5x0_nor2 PmosMod W=100u L=1u
        Mn0_5x0_nnor1 n0_5x0_norout n0_5_1x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_5x0_pnor2 n0_5x0_nor2 n0_5_2x0_andout n0_5x0_nor3 n0_5x0_nor3 PmosMod W=100u L=1u
        Mn0_5x0_nnor2 n0_5x0_norout n0_5_2x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_5x0_pnor3 n0_5x0_nor3 n0_5_3x0_andout VDD VDD PmosMod W=100u L=1u
        Mn0_5x0_nnor3 n0_5x0_norout n0_5_3x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_5x0_pnot v1_1 n0_5x0_norout VDD VDD PmosMod W=100u L=1u
        Mn0_5x0_nnot v1_1 n0_5x0_norout 0 0 NmosMod W=100u L=1u


        Rn0_6_0_0_0x0_res selector_0 n0_6_0_0_0x0_baseout 1m
        Mn0_6_0_0x0_p n0_6_0_0x0_notout n0_6_0_0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_0_0x0_n n0_6_0_0x0_notout n0_6_0_0_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_6_0_1_0x0_res selector_1 n0_6_0_1_0x0_baseout 1m
        Mn0_6_0_1x0_p n0_6_0_1x0_notout n0_6_0_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_0_1x0_n n0_6_0_1x0_notout n0_6_0_1_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_6_0_2x0_res n0x0_case0_2 n0_6_0_2x0_baseout 1m
        Mn0_6_0x0_pnand0 n0_6_0x0_nandout n0_6_0_0x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_6_0x0_nnand0 n0_6_0x0_nandout n0_6_0_0x0_notout n0_6_0x0_nand1 n0_6_0x0_nand1 NmosMod W=100u L=1u
        Mn0_6_0x0_pnand1 n0_6_0x0_nandout n0_6_0_1x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_6_0x0_nnand1 n0_6_0x0_nand1 n0_6_0_1x0_notout n0_6_0x0_nand2 n0_6_0x0_nand2 NmosMod W=100u L=1u
        Mn0_6_0x0_pnand2 n0_6_0x0_nandout n0_6_0_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_0x0_nnand2 n0_6_0x0_nand2 n0_6_0_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_6_0x0_pnot n0_6_0x0_andout n0_6_0x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_6_0x0_nnot n0_6_0x0_andout n0_6_0x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_6_1_0x0_res selector_0 n0_6_1_0x0_baseout 1m
        Rn0_6_1_1_0x0_res selector_1 n0_6_1_1_0x0_baseout 1m
        Mn0_6_1_1x0_p n0_6_1_1x0_notout n0_6_1_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_1_1x0_n n0_6_1_1x0_notout n0_6_1_1_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_6_1_2x0_res n0x0_case1_2 n0_6_1_2x0_baseout 1m
        Mn0_6_1x0_pnand0 n0_6_1x0_nandout n0_6_1_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_1x0_nnand0 n0_6_1x0_nandout n0_6_1_0x0_baseout n0_6_1x0_nand1 n0_6_1x0_nand1 NmosMod W=100u L=1u
        Mn0_6_1x0_pnand1 n0_6_1x0_nandout n0_6_1_1x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_6_1x0_nnand1 n0_6_1x0_nand1 n0_6_1_1x0_notout n0_6_1x0_nand2 n0_6_1x0_nand2 NmosMod W=100u L=1u
        Mn0_6_1x0_pnand2 n0_6_1x0_nandout n0_6_1_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_1x0_nnand2 n0_6_1x0_nand2 n0_6_1_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_6_1x0_pnot n0_6_1x0_andout n0_6_1x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_6_1x0_nnot n0_6_1x0_andout n0_6_1x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_6_2_0_0x0_res selector_0 n0_6_2_0_0x0_baseout 1m
        Mn0_6_2_0x0_p n0_6_2_0x0_notout n0_6_2_0_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_2_0x0_n n0_6_2_0x0_notout n0_6_2_0_0x0_baseout 0 0 NmosMod W=100u L=1u

        Rn0_6_2_1x0_res selector_1 n0_6_2_1x0_baseout 1m
        Rn0_6_2_2x0_res n0x0_case2_2 n0_6_2_2x0_baseout 1m
        Mn0_6_2x0_pnand0 n0_6_2x0_nandout n0_6_2_0x0_notout VDD VDD PmosMod W=100u L=1u
        Mn0_6_2x0_nnand0 n0_6_2x0_nandout n0_6_2_0x0_notout n0_6_2x0_nand1 n0_6_2x0_nand1 NmosMod W=100u L=1u
        Mn0_6_2x0_pnand1 n0_6_2x0_nandout n0_6_2_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_2x0_nnand1 n0_6_2x0_nand1 n0_6_2_1x0_baseout n0_6_2x0_nand2 n0_6_2x0_nand2 NmosMod W=100u L=1u
        Mn0_6_2x0_pnand2 n0_6_2x0_nandout n0_6_2_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_2x0_nnand2 n0_6_2x0_nand2 n0_6_2_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_6_2x0_pnot n0_6_2x0_andout n0_6_2x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_6_2x0_nnot n0_6_2x0_andout n0_6_2x0_nandout 0 0 NmosMod W=100u L=1u

        Rn0_6_3_0x0_res selector_0 n0_6_3_0x0_baseout 1m
        Rn0_6_3_1x0_res selector_1 n0_6_3_1x0_baseout 1m
        Rn0_6_3_2x0_res n0x0_case3_2 n0_6_3_2x0_baseout 1m
        Mn0_6_3x0_pnand0 n0_6_3x0_nandout n0_6_3_0x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_3x0_nnand0 n0_6_3x0_nandout n0_6_3_0x0_baseout n0_6_3x0_nand1 n0_6_3x0_nand1 NmosMod W=100u L=1u
        Mn0_6_3x0_pnand1 n0_6_3x0_nandout n0_6_3_1x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_3x0_nnand1 n0_6_3x0_nand1 n0_6_3_1x0_baseout n0_6_3x0_nand2 n0_6_3x0_nand2 NmosMod W=100u L=1u
        Mn0_6_3x0_pnand2 n0_6_3x0_nandout n0_6_3_2x0_baseout VDD VDD PmosMod W=100u L=1u
        Mn0_6_3x0_nnand2 n0_6_3x0_nand2 n0_6_3_2x0_baseout 0 0 NmosMod W=100u L=1u
        Mn0_6_3x0_pnot n0_6_3x0_andout n0_6_3x0_nandout VDD VDD PmosMod W=100u L=1u
        Mn0_6_3x0_nnot n0_6_3x0_andout n0_6_3x0_nandout 0 0 NmosMod W=100u L=1u

        Mn0_6x0_pnor0 n0_6x0_norout n0_6_0x0_andout n0_6x0_nor1 n0_6x0_nor1 PmosMod W=100u L=1u
        Mn0_6x0_nnor0 n0_6x0_norout n0_6_0x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_6x0_pnor1 n0_6x0_nor1 n0_6_1x0_andout n0_6x0_nor2 n0_6x0_nor2 PmosMod W=100u L=1u
        Mn0_6x0_nnor1 n0_6x0_norout n0_6_1x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_6x0_pnor2 n0_6x0_nor2 n0_6_2x0_andout n0_6x0_nor3 n0_6x0_nor3 PmosMod W=100u L=1u
        Mn0_6x0_nnor2 n0_6x0_norout n0_6_2x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_6x0_pnor3 n0_6x0_nor3 n0_6_3x0_andout VDD VDD PmosMod W=100u L=1u
        Mn0_6x0_nnor3 n0_6x0_norout n0_6_3x0_andout 0 0 NmosMod W=100u L=1u
        Mn0_6x0_pnot v1_2 n0_6x0_norout VDD VDD PmosMod W=100u L=1u
        Mn0_6x0_nnot v1_2 n0_6x0_norout 0 0 NmosMod W=100u L=1u
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
    }
}