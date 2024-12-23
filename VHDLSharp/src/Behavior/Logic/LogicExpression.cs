namespace VHDLSharp;

/// <summary>
/// Logical expression that can be used in a logical behavior
/// </summary>
public abstract class LogicExpression
{
    /// <summary>
    /// Get all signals used in the expression
    /// </summary>
    public abstract IEnumerable<ISignal> Signals { get; }

    /// <summary>
    /// Get in VHDL format
    /// </summary>
    public abstract string ToVhdl { get; }

    /// <summary>
    /// Dimension of this signal--must be the same for all inputs
    /// If null, then it can be any dimension (booleans)
    /// </summary>
    public abstract int? Dimension { get; }

    /// <summary>
    /// Generate an And with this expression and another
    /// Used for chaining expressions (exp1.And(exp2))
    /// </summary>
    /// <param name="logicExpression"></param>
    /// <returns></returns>
    public And And(LogicExpression logicExpression) => new(this, logicExpression);

    /// <summary>
    /// Generate an Or with this expression and another
    /// Used for chaining expressions (exp1.Or(exp2))
    /// </summary>
    /// <param name="logicExpression"></param>
    /// <returns></returns>
    public Or Or(LogicExpression logicExpression) => new(this, logicExpression);

    /// <summary>
    /// Generate a Not from this expressions
    /// Used for chaining expressions (exp1.Not())
    /// </summary>
    /// <returns></returns>
    public Not Not() => new(this);

    /// <summary>
    /// Module this expression is in
    /// </summary>
    public Module Parent => Signals.First().Parent;

    /// <summary>
    /// Convert boolean to logic expression
    /// </summary>
    /// <param name="b"></param>
    public static implicit operator LogicExpression(bool b) => new BooleanExpression(b);

    /// <summary>
    /// Convert signal to logical expression
    /// </summary>
    /// <param name="signal"></param>
    public static implicit operator LogicExpression(SingleNodeSignal signal) => (SignalExpression)signal;
}