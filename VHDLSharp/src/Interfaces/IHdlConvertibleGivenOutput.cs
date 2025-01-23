using VHDLSharp.Signals;

namespace VHDLSharp;

/// <summary>
/// Interface for anything that can be converted to Spice or VHDL given output signal (and unique id for Spice)
/// </summary>
public interface IHdlConvertibleGivenOutput
{
    /// <summary>
    /// Convert to VHDL given output signal
    /// </summary>
    /// <param name="outputSignal"></param>
    /// <returns></returns>
    public string ToVhdl(NamedSignal outputSignal);

    /// <summary>
    /// Convert to Spice given output signal and unique id
    /// </summary>
    /// <param name="outputSignal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public string ToSpice(NamedSignal outputSignal, string uniqueId);
}