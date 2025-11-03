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

    /// <summary>
    /// Convert integer to dimension
    /// </summary>
    /// <param name="dimension"></param>
    public static implicit operator DefiniteDimension(int dimension) => new(dimension);

    /// <summary>
    /// Convert dimension to integer
    /// </summary>
    /// <param name="dimension"></param>
    public static implicit operator int(DefiniteDimension dimension) => dimension.NonNullValue;
}