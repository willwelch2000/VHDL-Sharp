namespace VHDLSharp.Simulations;

/// <summary>
/// Stimulus that applies a multi-dimensional step stimulus. It starts with all stimuli
/// at 0, and then the binary value increases by 1 until it reaches the max and goes back to 0.
/// It is possible to add stimuli after initialization, but it is advised to not do so.
/// The stimuli are set whenever the value is adjusted. 
/// </summary>
public class MultiDimensionalStepStimulus : MultiDimensionalStimulus
{
    private readonly int dimension;

    private double pulseWidth;

    /// <summary>
    /// Create step stimulus where the binary value steps through all values from 0 to its max. 
    /// </summary>
    /// <param name="pulseWidth">Length of each step</param>
    /// <param name="dimension">Number of bits</param>
    public MultiDimensionalStepStimulus(double pulseWidth, int dimension)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(dimension, 1, nameof(dimension));
        ArgumentOutOfRangeException.ThrowIfLessThan(pulseWidth, 0, nameof(pulseWidth));
        this.pulseWidth = pulseWidth;
        this.dimension = dimension;
        SetStimuli();
    }

    /// <summary>
    /// Length in seconds of each step in the step function
    /// </summary>
    public double PulseWidth
    {
        get => pulseWidth;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, nameof(PulseWidth));
            pulseWidth = value;
            SetStimuli();
        }
    }

    private void SetStimuli()
    {
        // Set stimuli to individual bits of value
        Stimuli.Clear();
        for (int i = 0; i < dimension; i++)
        {
            double multiplier = 1 << i;
            Stimuli.Add(new PulseStimulus(multiplier*pulseWidth, multiplier*pulseWidth, 2*multiplier*pulseWidth));
        }
    }
}