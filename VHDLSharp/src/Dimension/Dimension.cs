namespace VHDLSharp.Dimensions;

/// <summary>
/// Dimension specification for signals, etc.
/// </summary>
/// <param name="value">If known, the dimension</param>
/// <param name="minimum">If known, the minimum that the dimension could be</param>
/// <param name="maximum">If known, the maximum that the dimension could be</param>
public class Dimension(int? value, int? minimum, int? maximum) : IEquatable<Dimension>
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
    public Dimension(int? minimum, int? maximum) : this(null, minimum, maximum)
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

    /// <summary>
    /// Generate a single dimension object by combining many
    /// Checks for validity
    /// </summary>
    /// <param name="dimensions"></param>
    /// <returns></returns>
    public static Dimension Combine(IEnumerable<Dimension> dimensions)
    {
        if (!AreCompatible(dimensions))
            throw new Exception("Dimensions are incompatible");
            
        return CombineWithoutCheck(dimensions);
    }

    internal static Dimension CombineWithoutCheck(IEnumerable<Dimension> dimensions)
    {
        // Value, if present
        if (dimensions.FirstOrDefault(d => d.Value is not null) is Dimension dimension)
            return new(dimension.Value ?? 0);

        // Minimum
        IEnumerable<int?> minimums = dimensions.Select(d => d.Minimum).Where(m => m is not null);
        int? minimum = null;
        if (minimums.Any())
            minimum = minimums.Max();

        // Maximum
        IEnumerable<int?> maximums = dimensions.Select(d => d.Maximum).Where(m => m is not null);
        int? maximum = null;
        if (maximums.Any())
            maximum = maximums.Max();

        return new(minimum, maximum);
    }

    /// <summary>
    /// Check if many dimensions are compatible together
    /// </summary>
    /// <param name="dimensions"></param>
    /// <returns></returns>
    public static bool AreCompatible(IEnumerable<Dimension> dimensions)
    {
        if (dimensions.Count() < 2)
            return true;

        Dimension[] array = dimensions.ToArray();
        for (int i = 0; i < array.Length-1; i++)
        {
            Dimension first = array[i];
            for (int j = i+1; j < array.Length; j++)
            {
                Dimension second = array[j];
                if (!first.Compatible(second))
                    return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public bool Equals(Dimension? other) => other is not null && Value == other.Value && Minimum == other.Minimum && Maximum == other.Maximum;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Dimension dim && Equals(dim);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        if (Value is not null)
            hash.Add(Value);
        if (Minimum is not null)
            hash.Add(Minimum);
        if (Maximum is not null)
            hash.Add(Maximum);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Return true if two dimensions are equal
    /// </summary>
    /// <param name="dimension1"></param>
    /// <param name="dimension2"></param>
    /// <returns></returns>
    public static bool operator==(Dimension dimension1, Dimension dimension2) => dimension1.Equals(dimension2);

    /// <summary>
    /// Return true if two dimensions are not equal
    /// </summary>
    /// <param name="dimension1"></param>
    /// <param name="dimension2"></param>
    /// <returns></returns>
    public static bool operator!=(Dimension dimension1, Dimension dimension2) => !dimension1.Equals(dimension2);
}