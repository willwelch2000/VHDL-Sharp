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

    /// <summary>
    /// Convert ot logical expression
    /// </summary>
    /// <param name="signal"></param>
    public static implicit operator LogicExpression(SingleNodeSignal signal) => signal.ToLogicExpression;
}