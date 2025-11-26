using VHDLSharp.BuiltIn;
using VHDLSharp.Dimensions;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals.Derived;

/// <summary>
/// A slice of a signal
/// </summary>
public class SliceSignal : DerivedSignal
{
    /// <summary>
    /// Generate a slice of an input signal
    /// </summary>
    /// <param name="inputSignal">Input signal to be sliced</param>
    /// <param name="start">Inclusive start of slice</param>
    /// <param name="end">Exclusive end of slice</param>
    /// <exception cref="ArgumentException">If start or end are invalid</exception>
    public SliceSignal(IModuleSpecificSignal inputSignal, int start, int end) : base(inputSignal.ParentModule)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0, nameof(start));
        if (start >= inputSignal.Dimension.NonNullValue)
            throw new ArgumentException($"Start must be less than number of input bits");
        if (start >= end)
            throw new ArgumentException($"Start must be less than end");
        if (end > inputSignal.Dimension.NonNullValue)
            throw new ArgumentException($"End must be less than or equal to number of input bits");
        InputSignal = inputSignal;
        Start = start;
        End = end;
    }

    /// <summary>Input signal from which the slice is taken</summary>
    public IModuleSpecificSignal InputSignal { get; }

    /// <summary>Inclusive start of slice</summary>
    public int Start { get; }

    /// <summary>Exclusive end of slice</summary>
    public int End { get; }

    /// <inheritdoc/>
    public override DefiniteDimension Dimension => End - Start;

    /// <inheritdoc/>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => [InputSignal];

    /// <inheritdoc/>
    protected override IInstantiation CompileWithoutCheck(string moduleName, string instanceName)
    {
        Instantiation inst = new(new Slice(InputSignal.Dimension.NonNullValue, Start, End), ParentModule, instanceName);
        inst.PortMapping.SetPort("Input", InputSignal.AsNamedSignal());
        inst.PortMapping.SetPort("Output", GetLinkedSignal());
        return inst;
    }
}