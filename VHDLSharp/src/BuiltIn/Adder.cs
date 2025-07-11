using VHDLSharp.Modules;

namespace VHDLSharp.BuiltIn;

/// <summary>
/// Adder module, with a given number of bits
/// Ports: A, B, Y, COut
/// </summary>
/// <param name="bits"></param>
public class Adder(int bits) : ParameterizedModule<int>(bits)
{
    /// <inheritdoc/>
    public override IModule BuildModule(int input)
    {
        throw new NotImplementedException();
    }
}