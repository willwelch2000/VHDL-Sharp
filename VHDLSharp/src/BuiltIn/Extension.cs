using VHDLSharp.Behaviors;
using VHDLSharp.Modules;
using VHDLSharp.Signals;

namespace VHDLSharp.BuiltIn;

/// <summary>
/// Parameter set for <see cref="Extension"/> module
/// </summary>
/// <param name="InputBits">Number of bits for the input signal</param>
/// <param name="OutputBits">Number of bits for the output signal</param>
/// <param name="Signed">If true, the extension is a signed extension,
    /// so the added bits will match the MSB of the input signal</param>
public record struct ExtensionParams(int InputBits, int OutputBits, bool Signed) : IEquatable<ExtensionParams>;

/// <summary>
/// Module to extend an input signal to a certain output length, either in signed or unsigned notation
/// </summary>
public class Extension : ParameterizedModule<ExtensionParams>
{
    /// <summary>
    /// Build Extension module given parameter set
    /// </summary>
    /// <param name="options"></param>
    public Extension(ExtensionParams options) : base(options) { }

    /// <summary>
    /// Build Extension module
    /// </summary>
    /// <param name="inputBits">Number of bits for the input signal</param>
    /// <param name="outputBits">Number of bits for the output signal</param>
    /// <param name="signed">If true, the extension is a signed extension,
    /// so the added bits will match the MSB of the input signal</param>
    public Extension(int inputBits, int outputBits, bool signed) : base(new(inputBits, outputBits, signed)) { }

    /// <inheritdoc/>
    public override IModule BuildModule(ExtensionParams options)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.InputBits, 1, nameof(options.InputBits));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.OutputBits, 1, nameof(options.OutputBits));
        if (options.InputBits >= options.OutputBits)
            throw new ArgumentException($"Number of output bits must be greater than number of input bits");

        string name = $"Extension_{options.InputBits}_{options.OutputBits}{(options.Signed ? "_signed" : "")}";
        Module module = new(name);

        Port inputPort = module.AddNewPort("Input", options.InputBits, PortDirection.Input);
        Port outputPort = module.AddNewPort("Output", options.OutputBits, PortDirection.Output);

        // Assign matching part of signal
        INamedSignal lower = outputPort.Signal[0..options.InputBits];
        module.SignalBehaviors[lower] = new LogicBehavior(inputPort.Signal);

        INamedSignal upper = outputPort.Signal[options.InputBits..];
        // If signed, copy MSB of input to all upper bits of output
        if (options.Signed)
        {
            ISingleNodeNamedSignal msb = inputPort.Signal[^1];
            foreach (ISingleNodeNamedSignal upperNode in upper.ToSingleNodeSignals)
                module.SignalBehaviors[upperNode] = new LogicBehavior(msb);
        }
        // If not signed, assign all 0s for remaining part
        else
            module.SignalBehaviors[upper] = new LogicBehavior(new Literal(0, options.OutputBits - options.InputBits));

        return module;
    }
}