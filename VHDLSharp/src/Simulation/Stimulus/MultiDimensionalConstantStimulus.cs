namespace VHDLSharp.Simulations;

/// <summary>
/// Stimulus that applies a multidimensional constant digital value.
/// It is possible to add stimuli after initialization, but it is advised to not do so.
/// The stimuli are set whenever the value is adjusted. 
/// </summary>
public class MultiDimensionalConstantStimulus : MultiDimensionalStimulus
{
    private readonly int dimension;

    private int value = 0;

    /// <summary>
    /// Default constructor
    /// </summary>
    public MultiDimensionalConstantStimulus() {}

    /// <summary>
    /// Constructor given value
    /// </summary>
    /// <param name="value"></param>
    /// <param name="dimension"></param>
    public MultiDimensionalConstantStimulus(int value, int dimension) : base()
    {
        this.value = value;
        this.dimension = dimension;
        SetStimuli();
    }

    /// <summary>
    /// Digital value of stimulus
    /// </summary>
    public int Value
    {
        get => value;
        set
        {
            this.value = value;
            SetStimuli();
        }
    }

    private void SetStimuli()
    {
        // Set stimuli to individual bits of value
        Stimuli.Clear();
        for (int i = 0; i < dimension; i++)
            Stimuli.Add(new ConstantStimulus((value & 1<<i) > 0));
    }
}