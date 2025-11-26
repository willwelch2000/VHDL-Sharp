using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.BuiltIn;

/// <summary>
/// Parameter set for <see cref="Slice"/> module
/// </summary>
/// <param name="InputBits">Number of bits for the input signal</param>
/// <param name="Start">Inclusive start of slice</param>
/// <param name="End">Exclusive end of slice</param>
public record struct SliceParams(int InputBits, int Start, int End) : IEquatable<SliceParams>;

/// <summary>
/// Module to output a slice of an input signal
/// </summary>
public class Slice : ParameterizedModule<SliceParams>
{
    /// <summary>
    /// Build Slice module--outputs a slice of an input signal
    /// </summary>
    /// <param name="options"></param>
    public Slice(SliceParams options) : base(options) { }

    /// <summary>
    /// Build Slice module--outputs a slice of an input signal
    /// </summary>
    /// <param name="inputBits">Number of bits for the input signal</param>
    /// <param name="start">Inclusive start of slice</param>
    /// <param name="end">Exclusive end of slice</param>
    public Slice(int inputBits, int start, int end) : base(new(inputBits, start, end)) { }

    /// <inheritdoc/>
    public override IModule BuildModule(SliceParams options)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.InputBits, 1, nameof(options.InputBits));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Start, 0, nameof(options.Start));
        if (options.Start >= options.InputBits)
            throw new ArgumentException($"Start must be less than number of input bits");
        if (options.Start >= options.End)
            throw new ArgumentException($"Start must be less than End");
        if (options.End > options.InputBits)
            throw new ArgumentException($"End must be less than or equal to number of input bits");

        string name = $"Slice_{options.InputBits}_{options.Start}_{options.End}";
        Module module = new(name);
        Port inputPort = module.AddNewPort("Input", options.InputBits, PortDirection.Input);
        Port outputPort = module.AddNewPort("Output", options.End - options.Start, PortDirection.Output);

        outputPort.Signal.AssignBehavior(inputPort.Signal[options.Start..options.End]);

        return module;
    }
}