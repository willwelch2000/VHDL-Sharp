using VHDLSharp.Behaviors;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class DynamicBehaviorTests
{
    [TestMethod]
    public void FlipFlopTest()
    {
        Module flipFlopMod = Util.GetFlipFlopModule();
        DynamicBehavior behavior = flipFlopMod.SignalBehaviors.Values.First() as DynamicBehavior ?? throw new();

        // Basic stuff
        Assert.AreEqual(flipFlopMod, behavior.ParentModule);
        Assert.AreEqual(new Dimension(1), behavior.Dimension);
        behavior.InitialValue = 2;
        Assert.IsFalse(behavior.ValidityManager.IsValid(out _));
        behavior.InitialValue = 0;
        Assert.IsTrue(behavior.ValidityManager.IsValid(out _));

        // Check simulation rule and its output values
        SubmoduleReference submoduleRef = new(flipFlopMod, []);
        SignalReference outRef = submoduleRef.GetChildSignalReference("OUT");
        SimulationRule simRule = behavior.GetSimulationRule(outRef);
        Assert.AreEqual(outRef, simRule.OutputSignal);
        Assert.AreEqual(0, simRule.IndependentEventTimeGenerator(1).Count());
        SignalReference loadRef = submoduleRef.GetChildSignalReference("LOAD");
        SignalReference inRef = submoduleRef.GetChildSignalReference("IN");
        RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint([], [0], 0);
        Assert.AreEqual(0, simRule.OutputValueCalculation(state));
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {loadRef, [0]},
            {inRef, [0]},
            {outRef, [0]},
        }, [0, 1], 1);
        Assert.AreEqual(0, simRule.OutputValueCalculation(state));
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {loadRef, [0, 1]},
            {inRef, [0, 1]},
            {outRef, [0, 0]},
        }, [0, 1, 2], 2);
        Assert.AreEqual(1, simRule.OutputValueCalculation(state));
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {loadRef, [0, 1]},
            {inRef, [0, 0]},
            {outRef, [0, 0]},
        }, [0, 1, 2], 2);
        Assert.AreEqual(0, simRule.OutputValueCalculation(state));
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {loadRef, [0, 1, 1, 0, 1]},
            {inRef, [0, 1, 1, 0, 0]},
            {outRef, [0, 0, 1, 1, 1]},
        }, [0, 1, 2, 3, 4, 5], 5);
        Assert.AreEqual(0, simRule.OutputValueCalculation(state));

        // Test initial value
        behavior.InitialValue = 1;
        simRule = behavior.GetSimulationRule(outRef);
        state = RuleBasedSimulationState.GivenStartingPoint([], [0], 0);
        Assert.AreEqual(1, simRule.OutputValueCalculation(state));
    }
}