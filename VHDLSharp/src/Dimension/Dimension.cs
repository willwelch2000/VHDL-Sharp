namespace VHDLSharp;

/// <summary>
/// Dimension specification for signals, etc.
/// </summary>
/// <param name="value">If known, the dimension</param>
/// <param name="minimum">If known, the minimum that the dimension could be</param>
/// <param name="maximum">If known, the maximum that the dimension could be</param>
public class Dimension(int? value, int? minimum, int? maximum)
{
    /// <summary>
    /// Generate dimension given value
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Dimension(int value) : this(value, null, null)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException(nameof(value), "Must be > 0");
    }

    /// <summary>
    /// Generate dimension given minimum and/or maximum
    /// </summary>
    /// <param name="minimum"></param>
    /// <param name="maximum"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Dimension(int? minimum = null, int? maximum = null) : this(null, minimum, maximum)
    {
        if (minimum is not null && minimum < 0)
            throw new ArgumentOutOfRangeException(nameof(minimum), "Must be > 0");
        if (maximum is not null && maximum < 0)
            throw new ArgumentOutOfRangeException(nameof(maximum), "Must be > 0");
        if (minimum is not null && maximum is not null && minimum > maximum)
            throw new ArgumentException("Maximum must be larger than minimum");
    }

    /// <summary>
    /// Generate dimension with no specifications
    /// </summary>
    public Dimension() : this(null, null, null) {}

    /// <summary>
    /// If known, the dimension
    /// </summary>
    public int? Value { get; } = value;

    /// <summary>
    /// If known, the minimum that the dimension could be
    /// </summary>
    public int? Minimum { get; } = minimum;

    /// <summary>
    /// If known, the maximum that the dimension could be
    /// </summary>
    public int? Maximum { get; } = maximum;

    /// <summary>
    /// Check if another dimension is compatible with this one
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Compatible(Dimension other)
    {
        if (Value is not null)
        {
            if (other.Value is not null)
                return Value == other.Value;
            if (other.Minimum is not null && other.Minimum > Value)
                return false;
            if (other.Maximum is not null && other.Maximum < Value)
                return false;
            return true;
        }
        if (other.Value is not null)
        {
            if (Minimum is not null && Minimum > other.Value)
                return false;
            if (Maximum is not null && Maximum < other.Value)
                return false;
            return true;
        }
        if (Minimum is not null && other.Maximum is not null && Minimum > other.Maximum)
            return false;
        if (other.Minimum is not null && Maximum is not null && other.Minimum > Maximum)
            return false;
        return true;
    }
}