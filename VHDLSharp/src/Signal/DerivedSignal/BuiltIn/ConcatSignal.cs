using VHDLSharp.Behaviors;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals.Derived;

/// <summary>
/// A concatenation of two signals
/// </summary>
public class ConcatSignal : DerivedSignal
{
    /// <summary>
    /// Constructor given two signals
    /// </summary>
    /// <param name="upperSignal">Signal that makes up upper bits of output</param>
    /// <param name="lowerSignal">Signal that makes up lower bits of output</param>
    /// <exception cref="Exception">If signals are not compatible</exception>
    public ConcatSignal(IModuleSpecificSignal upperSignal, IModuleSpecificSignal lowerSignal) : base(upperSignal.ParentModule)
    {
        UpperSignal = upperSignal;
        LowerSignal = lowerSignal;
        if (!upperSignal.ParentModule.Equals(lowerSignal.ParentModule))
            throw new Exception($"Input signals to concat must be compatible");
        ManageNewSignals([upperSignal, lowerSignal]);
    }

    /// <summary>Signal that makes up upper bits of output</summary>
    public IModuleSpecificSignal UpperSignal { get; }

    /// <summary>Signal that makes up lower bits of output</summary>
    public IModuleSpecificSignal LowerSignal { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => new(UpperSignal.Dimension.NonNullValue + LowerSignal.Dimension.NonNullValue);

    /// <inheritdoc/>
    protected override IEnumerable<IModuleSpecificSignal> InputSignalsWithAssignedModule => [UpperSignal, LowerSignal];

    /// <inheritdoc/>
    protected override IInstantiation CompileWithoutCheck(string moduleName, string instanceName)
    {
        Module childModule = new(moduleName);
        int dimUpper = UpperSignal.Dimension.NonNullValue;
        int dimLower = LowerSignal.Dimension.NonNullValue;
        Port upper = childModule.AddNewPort("Upper", dimUpper, PortDirection.Input);
        Port lower = childModule.AddNewPort("Lower", dimLower, PortDirection.Input);
        Port output = childModule.AddNewPort("Output", dimUpper + dimLower, PortDirection.Output);
        childModule.SignalBehaviors[output.Signal[0..dimLower]] = new LogicBehavior(lower.Signal);
        childModule.SignalBehaviors[output.Signal[dimLower..]] = new LogicBehavior(upper.Signal);

        Instantiation inst = new(childModule, ParentModule, instanceName);
        inst.PortMapping[upper] = UpperSignal.AsNamedSignal();
        inst.PortMapping[lower] = LowerSignal.AsNamedSignal();
        inst.PortMapping[output] = GetLinkedSignal();
        return inst;
    }
}