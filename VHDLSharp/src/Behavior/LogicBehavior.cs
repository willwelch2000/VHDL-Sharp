
using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// A behavior that uses logical expressions
/// </summary>
public class LogicBehavior : CombinationalBehavior
{
    private readonly ISignal outputSignal;

    /// <summary>
    /// Generate logic behavior
    /// </summary>
    /// <param name="outputSignal"></param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public LogicBehavior(ISignal outputSignal, ILogicallyCombinable<ISignal> logicExpression)
    {
        // First object can be used because they all must have same dimension
        if (!logicExpression.CanCombine(outputSignal))
            throw new Exception("Output signal not compatible with logic expression");
        this.outputSignal = outputSignal;
        this.LogicExpression = logicExpression;
    }

    /// <summary>
    /// Output signal
    /// </summary>
    public override ISignal OutputSignal => outputSignal;

    /// <summary>
    /// The logical expression that this refers to
    /// </summary>
    public ILogicallyCombinable<ISignal> LogicExpression { get; }

    /// <summary>
    /// The input signals used in this behavior
    /// </summary>
    public override IEnumerable<ISignal> InputSignals
    {
        get
        {
            if (LogicExpression is ISignal signal)
                return [signal];
            if (LogicExpression is LogicTree<ISignal> tree)
                return tree.AllBaseObjects;
            return [];
        }
    }

    /// <inheritdoc/>
    public override string ToVhdl => $"{outputSignal} <= {LogicExpression.ToLogicString()};";
}