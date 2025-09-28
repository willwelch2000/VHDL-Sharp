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
    /// <exception cref="Exception">If signals are not compatible</exception>
    public AddedSignal(IModuleSpecificSignal signal1, IModuleSpecificSignal signal2) : base(signal1.ParentModule)
    {
        Signal1 = signal1;
        Signal2 = signal2;
        // Go ahead and throw exception if not compatible--no need to check validity later, bc dimension and parent modules won't change
        if (!signal1.CanCombine(signal2))
            throw new Exception($"Input signals to addition must be compatible");
    }

    /// <summary>First signal to add</summary>
    public IModuleSpecificSignal Signal1 { get; }

    /// <summary>Second signal to add</summary>
    public IModuleSpecificSignal Signal2 { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => Signal1.Dimension;

    /// <inheritdoc/>
    protected override IEnumerable<IModuleSpecificSignal> InputSignalsWithAssignedModule => [Signal1, Signal2];

    /// <inheritdoc/>
    protected override IInstantiation CompileWithoutCheck(string moduleName, string instanceName)
    {
        IModule adder = new Adder(Dimension.NonNullValue, false, false);
        Instantiation inst = new(adder, ParentModule, instanceName);
        inst.PortMapping.SetPort("A", Signal1.AsNamedSignal());
        inst.PortMapping.SetPort("B", Signal2.AsNamedSignal());
        inst.PortMapping.SetPort("Y", ((IDerivedSignal)this).GetLinkedSignal());
        return inst;
    }
}