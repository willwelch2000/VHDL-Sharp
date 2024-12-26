namespace VHDLSharp;

/// <summary>
/// Base class for any signal that contains just a single node (not a vector)
/// </summary>
public abstract class SingleNodeSignal : ISignal
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract Module Parent { get; }

    /// <inheritdoc/>
    public int Dimension => 1;
    
    /// <summary>
    /// Convert to logical expression
    /// </summary>
    public LogicExpression ToLogicExpression => new SignalExpression(this);

    /// <inheritdoc/>
    public string VhdlType => "std_logic";

    /// <inheritdoc/>
    public string ToVhdl => $"signal {Name}\t: {VhdlType}";

    /// <inheritdoc/>
    public bool CanCombine(ISignal other) =>
        Dimension == other.Dimension && Parent == other.Parent;

    /// <inheritdoc/>
    public string ToLogicString() => Name;

    /// <summary>
    /// Convert signal to signal expression
    /// </summary>
    /// <param name="signal"></param>
    public static implicit operator SignalExpression(SingleNodeSignal signal) => new(signal);
}