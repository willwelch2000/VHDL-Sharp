using VHDLSharp.BuiltIn;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals.Derived;

/// <summary>
/// Derived signal that subtracts one signal from another, using two's complement notation. 
/// Overflow is possible and is not checked for.
/// If this is an issue, make <see cref="ExtendedSignal"/> versions of the inputs
/// </summary>
public class SubtractedSignal : DerivedSignal
{
    /// <summary>
    /// Subtract two signals
    /// </summary>
    /// <param name="signal1">First signal (from which <paramref name="signal2"/> is subtracted)</param>
    /// <param name="signal2">Second signal (subtracted from <paramref name="signal1"/>)</param>
    /// <exception cref="Exception">If signals are not compatible</exception>
    public SubtractedSignal(IModuleSpecificSignal signal1, IModuleSpecificSignal signal2) : base(signal1.ParentModule)
    {
        Signal1 = signal1;
        Signal2 = signal2;
        // Go ahead and throw exception if not compatible--no need to check validity later, bc dimension and parent modules won't change
        if (!signal1.CanCombine(signal2))
            throw new Exception($"Input signals to subtraction must be compatible");
        ManageNewSignals([signal1, signal2]);
    }

    /// <summary>First signal (from which <see cref="Signal2"/> is subtracted)</summary>
    public IModuleSpecificSignal Signal1 { get; }

    /// <summary>Second signal (subtracted from <see cref="Signal1"/>)</summary>
    public IModuleSpecificSignal Signal2 { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => Signal1.Dimension;

    /// <inheritdoc/>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => [Signal1, Signal2];

    /// <inheritdoc/>
    protected override IInstantiation CompileWithoutCheck(string moduleName, string instanceName)
    {
        int dimension = Dimension.NonNullValue;
        // Define module
        Module module = new(moduleName);
        Port a = module.AddNewPort("A", PortDirection.Input);
        Port b = module.AddNewPort("B", PortDirection.Input);
        Port y = module.AddNewPort("Y", PortDirection.Output);

        // Define intermediate signals
        INamedSignal inverted = module.GenerateSignalOrVector("Inv", dimension);
        INamedSignal carryIn = module.GenerateSignal("CIn");
        inverted.AssignBehavior(b.Signal.Not());
        carryIn.AssignBehavior(1);

        // Make adder instantiation
        IModule adder = new Adder(dimension, true, false);
        Instantiation adderInst = module.AddNewInstantiation(adder, "Adder");
        adderInst.PortMapping.SetPort("A", a.Signal);
        adderInst.PortMapping.SetPort("B", inverted);
        adderInst.PortMapping.SetPort("CIn", carryIn);
        adderInst.PortMapping.SetPort("Y", y.Signal);

        // Top-level instantiation
        Instantiation inst = new(module, ParentModule, instanceName);
        inst.PortMapping.SetPort("A", Signal1.AsNamedSignal());
        inst.PortMapping.SetPort("B", Signal2.AsNamedSignal());
        inst.PortMapping.SetPort("Y", GetLinkedSignal());
        return inst;
    }
}