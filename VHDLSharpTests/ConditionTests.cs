using VHDLSharp.Conditions;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class ConditionTests
{
    [TestMethod]
    public void EvaluationTest()
    {
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;
        Module module1 = new("m1");
        Signal s1 = module1.GenerateSignal("s1");
        Signal s2 = module1.GenerateSignal("s2");
        Vector v1 = module1.GenerateVector("v1", 3);
        Vector v2 = module1.GenerateVector("v2", 3);
        Vector v3 = module1.GenerateVector("v3", 2);

        Equality equalitySingle = new(s1, s2);
        Assert.ThrowsException<IncompatibleSignalException>(() => new Equality(v1, v3));
        Equality equalityVector = v1.EqualityWith(v2);
        RisingEdge risingEdge = new(s1);
        FallingEdge fallingEdge = s1.FallingEdge;

        SubcircuitReference context = new(module1, []);
        SignalReference s1Ref = context.GetChildSignalReference(s1);
        SignalReference s2Ref = context.GetChildSignalReference(s2);
        SignalReference v1Ref = context.GetChildSignalReference(v1);
        SignalReference v2Ref = context.GetChildSignalReference(v2);

        // First state--nothing happened yet
        RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint([], [0], 0);
        Assert.IsFalse(equalitySingle.Evaluate(state, context));
        Assert.IsFalse(equalityVector.Evaluate(state, context));
        Assert.IsFalse(risingEdge.Evaluate(state, context));
        Assert.IsFalse(fallingEdge.Evaluate(state, context));

        // Second state--signals at 0
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {s1Ref, [0]},
            {s2Ref, [0]},
            {v1Ref, [0]},
            {v2Ref, [0]},
        }, [0, 1], 1);
        Assert.IsTrue(equalitySingle.Evaluate(state, context));
        Assert.IsTrue(equalityVector.Evaluate(state, context));
        Assert.IsFalse(risingEdge.Evaluate(state, context));
        Assert.IsFalse(fallingEdge.Evaluate(state, context));

        // Third state--s2 and v2 moved up
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {s1Ref, [0, 0]},
            {s2Ref, [0, 1]},
            {v1Ref, [0, 0]},
            {v2Ref, [0, 5]},
        }, [0, 1, 2], 2);
        Assert.IsFalse(equalitySingle.Evaluate(state, context));
        Assert.IsFalse(equalityVector.Evaluate(state, context));
        Assert.IsFalse(risingEdge.Evaluate(state, context));
        Assert.IsFalse(fallingEdge.Evaluate(state, context));

        // Fourth state--s1 and v1 moved up to match
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {s1Ref, [0, 0, 1]},
            {s2Ref, [0, 1, 1]},
            {v1Ref, [0, 0, 5]},
            {v2Ref, [0, 5, 5]},
        }, [0, 1, 2, 3], 3);
        Assert.IsTrue(equalitySingle.Evaluate(state, context));
        Assert.IsTrue(equalityVector.Evaluate(state, context));
        Assert.IsTrue(risingEdge.Evaluate(state, context));
        Assert.IsFalse(fallingEdge.Evaluate(state, context));

        // Fifth state--s1 moved back down
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {s1Ref, [0, 0, 1, 0]},
            {s2Ref, [0, 1, 1, 1]},
            {v1Ref, [0, 0, 5, 5]},
            {v2Ref, [0, 5, 5, 5]},
        }, [0, 1, 2, 3, 4], 4);
        Assert.IsFalse(equalitySingle.Evaluate(state, context));
        Assert.IsTrue(equalityVector.Evaluate(state, context));
        Assert.IsFalse(risingEdge.Evaluate(state, context));
        Assert.IsTrue(fallingEdge.Evaluate(state, context));
    }
}