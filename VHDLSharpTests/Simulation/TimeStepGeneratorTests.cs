using VHDLSharp.Modules;
using VHDLSharp.Simulations;

namespace VHDLSharpTests;

[TestClass]
public class TimeStepGeneratorTests
{
    [TestMethod]
    public void MainTest()
    {
        Module module1 = Util.GetSampleModule1();
        SubmoduleReference submodule = new(module1, []);
        SignalReference s1 = submodule.GetChildSignalReference("s1");
        var generator = new DefaultTimeStepGenerator {MinTimeStep = 1e-6, MaxTimeStep = null};
        double[] independentEventTimes = [1e-4, 2e-4];

        // State has changed since last timestep--move min time step
        RuleBasedSimulationState state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {s1, [0, 1]}
        }, [0, 1e-5], 1e-5);
        double[] nextSteps = [.. generator.NextTimeSteps(state, independentEventTimes, 1e-3)];
        Assert.AreEqual(1, nextSteps.Length);
        Assert.AreEqual(1.1e-5, nextSteps[0], 1e-8);

        // State has not changed--go to next independent time step - min time step
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {s1, [0, 1, 1]}
        }, [0, 1e-5, 1.1e-5], 1.1e-5);
        nextSteps = [.. generator.NextTimeSteps(state, independentEventTimes, 1e-3)];
        Assert.AreEqual(3, nextSteps.Length);
        Assert.AreEqual(0.99e-4, nextSteps[0], 1e-8);
        Assert.AreEqual(1.00e-4, nextSteps[1], 1e-8);
        Assert.AreEqual(1.01e-4, nextSteps[2], 1e-8);

        // State has not changed, but we have a max time step
        generator.MaxTimeStep = 1e-5;
        nextSteps = [.. generator.NextTimeSteps(state, independentEventTimes, 1e-3)];
        Assert.AreEqual(1, nextSteps.Length);
        Assert.AreEqual(2.1e-5, nextSteps[0], 1e-8);

        // State has not changed, and max time step would go past next independent event time
        state = RuleBasedSimulationState.GivenStartingPoint(new()
        {
            {s1, [0, 1, 1]}
        }, [0, 1e-5, 0.95e-4], 0.95e-4);
        nextSteps = [.. generator.NextTimeSteps(state, independentEventTimes, 1e-3)];
        Assert.AreEqual(3, nextSteps.Length);
        Assert.AreEqual(0.99e-4, nextSteps[0], 1e-8);
        Assert.AreEqual(1.00e-4, nextSteps[1], 1e-8);
        Assert.AreEqual(1.01e-4, nextSteps[2], 1e-8);
    }
}