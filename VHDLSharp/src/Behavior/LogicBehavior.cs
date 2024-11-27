
namespace VHDLSharp;

/// <summary>
/// A behavior that uses logical expressions
/// </summary>
/// <param name="outputSignal"></param>
/// <param name="logicExpression"></param>
public class LogicBehavior(SingleNodeSignal outputSignal, LogicExpression logicExpression) : DigitalBehavior
{
    /// <summary>
    /// Output signal
    /// </summary>
    public override ISignal OutputSignal => outputSignal;

    /// <summary>
    /// The logical expression that this refers to
    /// </summary>
    public LogicExpression LogicExpression { get; private set; } = logicExpression;

    /// <summary>
    /// The input signals used in this behavior
    /// </summary>
    public override IEnumerable<ISignal> InputSignals => LogicExpression.Signals;
}