namespace VHDLSharp;

/// <summary>
/// A signal used in a module
/// </summary>
/// <param name="Name">name of signal</param>
/// <param name="Parent">module to which this signal belongs</param>
public record struct Signal(string Name, Module Parent)
{
    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public override readonly string ToString() => Name;
}