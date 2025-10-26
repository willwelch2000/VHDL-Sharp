using VHDLSharp.Behaviors;
using VHDLSharp.Conditions;
using VHDLSharp.LogicTree;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.BuiltIn;

/// <summary>Parameters for creating <see cref="DFlipFlop"/></summary>
public class DFlipFlopParams : IEquatable<DFlipFlopParams>
{
    /// <summary>Trigger on the negative edge rather than the positive</summary>
    public bool NegativeEdgeTriggered { get; set; } = false;

    /// <summary>Include asynchronous set pin</summary>
    public bool AsyncSet { get; set; } = false;

    /// <summary>Include asynchronous reset pin</summary>
    public bool AsyncReset { get; set; } = false;

    /// <summary>Include enable pin</summary>
    public bool Enable { get; set; } = false;

    /// <inheritdoc/>
    public bool Equals(DFlipFlopParams? other) => other is not null &&
        other.NegativeEdgeTriggered == NegativeEdgeTriggered &&
        other.AsyncSet == AsyncSet &&
        other.AsyncReset == AsyncReset &&
        other.Enable == Enable;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DFlipFlopParams other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        (NegativeEdgeTriggered ? 1 : 0) +
        (AsyncSet ? 2 : 0) +
        (AsyncReset ? 4 : 0) +
        (Enable ? 8 : 0);
}

/// <summary>
/// D flip flop module, with <see cref="DFlipFlopParams"/> object as options.
/// Ports: D, CLK, Q, AsyncSet (optional), AsyncReset (optional), Enable (optional)
/// </summary>
/// <param name="options"></param>
public class DFlipFlop(DFlipFlopParams options) : ParameterizedModule<DFlipFlopParams>(options)
{
    /// <inheritdoc/>
    public override IModule BuildModule(DFlipFlopParams options)
    {
        // Name
        string name = "DFF";
        if (options.NegativeEdgeTriggered)
            name += "_NegEdge";
        if (options.AsyncSet)
            name += "_S";
        if (options.AsyncReset)
            name += "_R";
        if (options.Enable)
            name += "_En";
        Module module = new(name);

        // Main ports
        Signal d = module.GenerateSignal("D");
        Signal clk = module.GenerateSignal("CLK");
        Signal q = module.GenerateSignal("Q");
        module.AddNewPort(d, PortDirection.Input);
        module.AddNewPort(clk, PortDirection.Input);
        module.AddNewPort(q, PortDirection.Output);

        DynamicBehavior behavior = new();
        q.AssignBehavior(behavior);

        // Edge behavior
        ILogicallyCombinable<ICondition> edge = options.NegativeEdgeTriggered ? clk.FallingEdge() : clk.RisingEdge();
        if (options.Enable)
        {
            Signal en = module.GenerateSignal("Enable");
            module.AddNewPort(en, PortDirection.Input);
            edge = edge.And(en.IsHigh());
        }
        behavior.ConditionMappings.Add((edge, (LogicBehavior)d));

        // Async set/reset
        if (options.AsyncSet)
        {
            Signal set = module.GenerateSignal("AsyncSet");
            module.AddNewPort(set, PortDirection.Input);
            behavior.ConditionMappings.Add((set.IsHigh(), new ValueBehavior(1)));
        }
        if (options.AsyncReset)
        {
            Signal reset = module.GenerateSignal("AsyncReset");
            module.AddNewPort(reset, PortDirection.Input);
            behavior.ConditionMappings.Add((reset.IsHigh(), new ValueBehavior(0)));
        }

        return module;
    }
}