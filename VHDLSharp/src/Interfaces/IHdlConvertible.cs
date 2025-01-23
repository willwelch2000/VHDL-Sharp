namespace VHDLSharp;

/// <summary>
/// Interface for anything that can be converted to Spice or VHDL
/// </summary>
public interface IHdlConvertible
{
    /// <summary>
    /// Convert to Spice
    /// </summary>
    /// <returns></returns>
    public string ToVhdl();

    /// <summary>
    /// Convert to VHDL
    /// </summary>
    /// <returns></returns>
    public string ToSpice();
}