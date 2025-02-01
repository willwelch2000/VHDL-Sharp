namespace VHDLSharp.Dimensions;

/// <summary>
/// Dimension object that has well-defined dimension
/// </summary>
/// <param name="value"></param>
public class DefiniteDimension(int value) : Dimension(value, null, null)
{
    /// <summary>
    /// Accessor for value that is known to be nonnull
    /// </summary>
    public int NonNullValue => Value ?? throw new("Impossible");
}