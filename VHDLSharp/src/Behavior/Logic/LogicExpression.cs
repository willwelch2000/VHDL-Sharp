namespace VHDLSharp;

/// <summary>
/// Logical expression that can be used in a logical behavior
/// </summary>
public abstract class LogicExpression
{
    /// <summary>
    /// Get all signals used in the expression
    /// </summary>
    public abstract IEnumerable<SingleNodeSignal> Signals { get; }

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
}