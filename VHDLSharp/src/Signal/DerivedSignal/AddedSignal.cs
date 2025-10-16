using VHDLSharp.BuiltIn;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Derived signal that adds together two input signals
/// </summary>
public class AddedSignal : DerivedSignal
{
    /// <summary>
    /// Constructor given two signals
    /// </summary>
    /// <param name="signal1">First signal</param>
    /// <param name="signal2">Second signal</param>
    /// <param name="includeCarryOut">If true, the signal has an additional bit that comes from the carry-out of the addition</param>
    /// <exception cref="Exception">If signals are not compatible</exception>
    public AddedSignal(IModuleSpecificSignal signal1, IModuleSpecificSignal signal2, bool includeCarryOut=false) : base(signal1.ParentModule)
    {
        Signal1 = signal1;
        Signal2 = signal2;
        IncludeCarryOut = includeCarryOut;
        // Go ahead and throw exception if not compatible--no need to check validity later, bc dimension and parent modules won't change
        if (!signal1.CanCombine(signal2))
            throw new Exception($"Input signals to addition must be compatible");
    }

    /// <summary>First signal to add</summary>
    public IModuleSpecificSignal Signal1 { get; }

    /// <summary>Second signal to add</summary>
    public IModuleSpecificSignal Signal2 { get; }

    /// <summary>If true, the signal has an additional bit that comes from the carry-out of the addition</summary>
    public bool IncludeCarryOut { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => IncludeCarryOut ? new(Signal1.Dimension.NonNullValue + 1) : Signal1.Dimension;

    /// <inheritdoc/>
    protected override IEnumerable<IModuleSpecificSignal> InputSignalsWithAssignedModule => [Signal1, Signal2];

    /// <inheritdoc/>
    protected override IInstantiation CompileWithoutCheck(string moduleName, string instanceName)
    {
        IModule adder = new Adder(Dimension.NonNullValue, false, IncludeCarryOut);
        Instantiation inst = new(adder, ParentModule, instanceName);
        INamedSignal linkedSignal = ((IDerivedSignal)this).GetLinkedSignal();
        inst.PortMapping.SetPort("A", Signal1.AsNamedSignal());
        inst.PortMapping.SetPort("B", Signal2.AsNamedSignal());
        inst.PortMapping.SetPort("Y", IncludeCarryOut ? linkedSignal[0..^1] : linkedSignal);
        if (IncludeCarryOut)
            inst.PortMapping.SetPort("COut", linkedSignal);
        return inst;
    }
}