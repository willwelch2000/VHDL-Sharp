using VHDLSharp.BuiltIn;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals.Derived;

/// <summary>
/// An extension of an input signal
/// </summary>
public class ExtendedSignal : DerivedSignal
{
    /// <summary>
    /// Constructor given input signal, number of output bits, and if it is signed
    /// </summary>
    /// <param name="inputSignal">Input signal to be extended</param>
    /// <param name="outputBits">Number of bits for output signal</param>
    /// <param name="signed">If true, the extension is a signed extension,
    /// so the added bits will match the MSB of the input signal</param>
    /// <exception cref="ArgumentException">If output bits is not greater than the input signal dimension</exception>
    public ExtendedSignal(IModuleSpecificSignal inputSignal, int outputBits, bool signed = false) : base(inputSignal.ParentModule)
    {
        if (outputBits <= inputSignal.Dimension.NonNullValue)
            throw new ArgumentException($"Number of output bits must be greater than the input signal's dimension ({inputSignal.Dimension.NonNullValue})", nameof(outputBits));
        InputSignal = inputSignal;
        OutputBits = outputBits;
        Signed = signed;
        ManageNewSignals([inputSignal]);
    }

    /// <summary>Input signal to be extended</summary>
    public IModuleSpecificSignal InputSignal { get; }

    /// <summary>Number of bits for output signal</summary>
    public int OutputBits { get; }

    /// <summary>If true, the extension is a signed extension,
    /// so the added bits will match the MSB of the input signal</summary>
    public bool Signed { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => OutputBits;

    /// <inheritdoc/>
    protected override IEnumerable<IModuleSpecificSignal> InputSignalsWithAssignedModule => [InputSignal];

    /// <inheritdoc/>
    protected override IInstantiation CompileWithoutCheck(string moduleName, string instanceName)
    {
        Instantiation inst = new(new Extension(InputSignal.Dimension, OutputBits, Signed), ParentModule, instanceName);
        inst.PortMapping.SetPort("Input", InputSignal.AsNamedSignal());
        inst.PortMapping.SetPort("Output", GetLinkedSignal());
        return inst;
    }
}