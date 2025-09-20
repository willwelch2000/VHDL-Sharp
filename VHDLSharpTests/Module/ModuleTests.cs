using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.Validation;

namespace VHDLSharpTests;

[TestClass]
public class ModuleTests
{
    [TestMethod]
    public void BasicTest()
    {
        Module m1 = new("m1");
        Assert.AreEqual("m1", m1.Name);

        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = new("s2", m1);
        Signal s3 = new("s3", m1);
        Signal s4 = new("s4", m1);

        IModuleSpecificSignal[] moduleSignals = [.. m1.AllModuleSignals];
        Assert.IsFalse(moduleSignals.Length != 0);

        m1.AddNewPort(s1, PortDirection.Input);
        m1.AddNewPort(s2, PortDirection.Input);
        m1.AddNewPort(s4, PortDirection.Output);
        Assert.IsFalse(m1.IsComplete(out string? reason));

        moduleSignals = [.. m1.AllModuleSignals];
        Assert.AreEqual(3, moduleSignals.Length);
        Assert.IsTrue(moduleSignals.Contains(s1));
        Assert.IsTrue(moduleSignals.Contains(s2));
        Assert.IsTrue(moduleSignals.Contains(s4));

        s3.AssignBehavior(s1.Not());
        m1.SignalBehaviors[s4] = new LogicBehavior(s3.And(s2));
        Assert.IsTrue(m1.IsComplete(out reason));

        moduleSignals = [.. m1.AllModuleSignals];
        Assert.AreEqual(4, moduleSignals.Length);
        Assert.IsTrue(moduleSignals.Contains(s3));

        // Check VHDL
        string vhdl = m1.GetVhdl();
        string expectedVhdl = 
        """
        library ieee
        use ieee.std_logic_1164.all;

        entity m1 is
            port (
                s1	: in	std_logic;
                s2	: in	std_logic;
                s4	: out	std_logic
            );
        end m1;

        architecture rtl of m1 is
            signal s3	: std_logic
        begin
            s3 <= (not (s1));
            s4 <= (s3 and s2);
        end rtl;
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedVhdl, vhdl));

        // Check SPICE
        string spice = m1.GetSpice().AsString();
        string expectedSpice = 
        $"""
        {Util.GetNotSubcircuitSpice(false)}
        {Util.GetAndSubcircuitSpice(2, false)}
        .MODEL NmosMod nmos W=0.0001 L=1E-06
        .MODEL PmosMod pmos W=0.0001 L=1E-06
        VVDD VDD 0 5

        Rn0_0_0x0_res s1 n0_0_0x0_baseout 0.001
        Xn0_0x0_or n0_0_0x0_baseout n0_0x0_notout NOT
        Rn0x0_connect n0_0x0_notout s3 0.001
        
        Rn1_0_0x0_res s3 n1_0_0x0_baseout 0.001
        Rn1_0_1x0_res s2 n1_0_1x0_baseout 0.001
        Xn1_0x0_and n1_0_0x0_baseout n1_0_1x0_baseout n1_0x0_andout AND2
        Rn1x0_connect n1_0x0_andout s4 0.001

        Rn2x0_floating s4 0 1000000000
        """;
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpice, spice));

        // Check SPICE as subcircuit
        string spiceSubcircuit = m1.GetSpice().AsSubcircuitString();
        string expectedSpiceSubcircuit = ".subckt m1 s1 s2 s4\n" + expectedSpice + "\n.ends m1";
        Assert.IsTrue(Util.AreEqualIgnoringWhitespace(expectedSpiceSubcircuit, spiceSubcircuit));
        
        // Check rules
        SimulationRule[] rules = [.. m1.GetSimulationRules()];
        SubcircuitReference m1Ref = new(m1, []);
        Assert.AreEqual(2, rules.Length);
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == m1Ref.GetChildSignalReference(s3)));
        Assert.IsTrue(rules.Any(r => r.OutputSignal.Ascend() == m1Ref.GetChildSignalReference(s4)));
    }

    [TestMethod]
    public void InvalidBehaviorTest()
    {
        Module m1 = new("m1");
        Module m2 = new("m2");

        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = new("s2", m1);
        Signal s3 = new("s3", m2);
        Signal s4 = new("s4", m1);

        m1.AddNewPort(s1, PortDirection.Input);
        m1.AddNewPort(s4, PortDirection.Output);

        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;
        Assert.ThrowsException<Exception>(() => s1.AssignBehavior(0)); // Input port
        Issue[] issues = [.. m1.ValidityManager.Issues()];
        Assert.AreEqual(1, issues.Length);
        Issue issue = issues[0];
        Assert.AreEqual(m1, issue.TopLevelEntity);
        Assert.AreEqual(0, issue.FaultChain.Count);
        Assert.AreEqual("Output signal (s1) must not be an input port", issue.Exception.Message);
        s1.RemoveBehavior(); // Undo action

        s2.AssignBehavior(0);
        ValueBehavior s4Behavior = new(1);
        s4.AssignBehavior(s4Behavior);


        Assert.ThrowsException<Exception>(() => s4.AssignBehavior(3)); // Too high dimension
        issues = [.. m1.ValidityManager.Issues()];
        Assert.AreEqual(1, issues.Length);
        issue = issues[0];
        Assert.AreEqual(m1, issue.TopLevelEntity);
        Assert.AreEqual(0, issue.FaultChain.Count);
        Assert.AreEqual("Behavior must be compatible with output signal", issue.Exception.Message);
        s4.AssignBehavior(s4Behavior); // Undo

        Assert.ThrowsException<Exception>(() => s4.AssignBehavior(s3.Not())); // Signal from another module as input
        s4.AssignBehavior(s4Behavior);
        Assert.ThrowsException<Exception>(() => m2.SignalBehaviors[s1] = new ValueBehavior(1)); // Assigning signal in wrong module
        s4.AssignBehavior(s4Behavior);
        
        Assert.AreEqual(2, m1.SignalBehaviors.Count);
    }

    [TestMethod]
    public void InvalidPortTest()
    {
        Module m1 = new("m1");
        Module m2 = new("m2");

        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = new("s2", m2);

        m1.AddNewPort(s1, PortDirection.Input);

        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;
        Port p1 = new(s1, PortDirection.Output);
        Assert.ThrowsException<Exception>(() => m1.Ports.Add(p1)); // Duplicate signal
        Issue[] issues = [.. m1.ValidityManager.Issues()];
        Assert.AreEqual(1, issues.Length);
        Assert.AreEqual(m1, issues[0].TopLevelEntity);
        Assert.AreEqual(0, issues[0].FaultChain.Count);
        Assert.AreEqual("The same signal (\"s1\") cannot be added as two different ports", issues[0].Exception.Message);
        m1.Ports.Remove(p1);

        Port p2 = new(s2, PortDirection.Input);
        Assert.ThrowsException<Exception>(() => m1.Ports.Add(p2)); // Signal from another module
        issues = [.. m1.ValidityManager.Issues()];
        Assert.AreEqual(1, issues.Length);
        Assert.AreEqual(m1, issues[0].TopLevelEntity);
        Assert.AreEqual(0, issues[0].FaultChain.Count);
        Assert.AreEqual("Signal s2 must have this module (m1) as parent", issues[0].Exception.Message);
        m1.Ports.Remove(p2);
    }

    [TestMethod]
    public void InvalidInstantiationTest()
    {
        Module m1 = new("m1");
        Module m2 = new("m2");
        Module m3 = new("m3");

        IInstantiation i1 = m1.AddNewInstantiation(m2, "i1");
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;

        // Duplicate instantiation
        Assert.ThrowsException<Exception>(() => m1.Instantiations.Add(i1));
        m1.Instantiations.Remove(i1);

        // Same module and name
        Assert.ThrowsException<Exception>(() => m1.AddNewInstantiation(m2, "i1"));
        m1.Instantiations.Remove(m1.Instantiations.First(i => i.Name == "i1" && i != i1));

        // Different module but same name
        Assert.ThrowsException<Exception>(() => m1.AddNewInstantiation(m3, "i1"));
        m1.Instantiations.Remove(m1.Instantiations.First(i => i.Name == "i1" && i != i1));

        // Try valid versions of these
        m1.AddNewInstantiation(m2, "i2");
        m1.AddNewInstantiation(m3, "i3");
        Assert.IsTrue(m1.Instantiations.Count == 3);
    }

    [TestMethod]
    public void ModuleUpdatedCallbackTest()
    {
        Module m1 = new("m1");
        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = m1.GenerateSignal("s2");
        Vector v1 = m1.GenerateVector("v1", 2);
        bool callback = false;
        bool childCallback = false;
        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdates;
        m1.Updated += (sender, e) => callback = true;
        m1.ValidityManager.ChangeDetectedInMainOrTrackedEntity += (sender, e) => childCallback = true;

        // Callback by assigning behavior
        Assert.IsFalse(callback);
        s1.AssignBehavior(true);
        Assert.IsTrue(callback);
        Assert.IsTrue(childCallback);

        // Callback by assigning new behavior
        callback = false;
        childCallback = false;
        CaseBehavior caseBehavior1 = new(v1);
        s1.AssignBehavior(caseBehavior1);
        Assert.IsTrue(callback);
        Assert.IsTrue(childCallback);

        // Child callback by changing behavior
        callback = false;
        childCallback = false;
        caseBehavior1.AddCase(0, new Literal(0, 1));
        Assert.IsFalse(callback);
        Assert.IsTrue(childCallback);

        // Replace with new case behavior, confirm that it does child callback with new but not old
        CaseBehavior caseBehavior2 = new(v1);
        s1.AssignBehavior(caseBehavior2);
        childCallback = false;
        caseBehavior1.AddCase(1, new Literal(0, 1));
        Assert.IsFalse(childCallback);
        caseBehavior2.AddCase(1, new Literal(0, 1));
        Assert.IsTrue(childCallback);

        // Test adding behavior to multiple, removing from one--it should still do the callback when changed
        s2.AssignBehavior(caseBehavior2);
        s1.AssignBehavior(caseBehavior1);
        childCallback = false;
        caseBehavior2.AddCase(1, new Literal(0, 1));
        Assert.IsTrue(childCallback);
    }
    
    [TestMethod]
    public void ParentChildBehaviorOverwriteTest()
    {
        Module m1 = new("m1");
        Vector v1 = m1.GenerateVector("v1", 2);
        VectorNode v1Node0 = v1[0];

        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;

        // Child overwriting parent
        v1.AssignBehavior(2);
        Assert.ThrowsException<Exception>(() => v1Node0.AssignBehavior(0));
        Issue[] issues = [.. m1.ValidityManager.Issues()];
        Assert.AreEqual(1, issues.Length);
        Assert.AreEqual(m1, issues[0].TopLevelEntity);
        Assert.AreEqual(0, issues[0].FaultChain.Count);
        Assert.AreEqual("Module defines an overlapping parent (v1) and child (v1[0]) output signal", issues[0].Exception.Message);
        v1Node0.RemoveBehavior();

        // Parent overwriting child
        v1.RemoveBehavior();
        v1Node0.AssignBehavior(0);
        Assert.ThrowsException<Exception>(() => v1.AssignBehavior(1));
        issues = [.. m1.ValidityManager.Issues()];
        Assert.AreEqual(1, issues.Length);
        Assert.AreEqual(m1, issues[0].TopLevelEntity);
        Assert.AreEqual(0, issues[0].FaultChain.Count);
        Assert.AreEqual("Module defines an overlapping parent (v1) and child (v1[0]) output signal", issues[0].Exception.Message);
        v1.RemoveBehavior();
    }

    [TestMethod]
    public void SignalDefinedTwiceTest()
    {
        Module m1 = new("parentMod");
        Signal s1 = m1.GenerateSignal("s1");
        Signal s2 = m1.GenerateSignal("s2");

        Module instanceMod = Util.GetSampleModule1();
        IPort i1p1 = instanceMod.Ports.ElementAt(0);
        IPort i1p2 = instanceMod.Ports.ElementAt(1);
        
        Instantiation i1 = new(instanceMod, m1, "i1");
        i1.PortMapping[i1p1] = s1;
        i1.PortMapping[i1p2] = s2;
        m1.Instantiations.Add(i1);

        ValidityManager.GlobalSettings.MonitorMode = MonitorMode.AlertUpdatesAndThrowException;

        // Behavior and instantiation output
        Assert.ThrowsException<Exception>(() => s2.AssignBehavior(true));
        s2.RemoveBehavior(); // Undo
        s1.AssignBehavior(false); // Should work since it's an input of instantiation, not output
    }
}