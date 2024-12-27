
using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// A behavior that uses logical expressions
/// </summary>
/// <param name="logicExpression"></param>
/// <exception cref="Exception"></exception>
public class LogicBehavior(ILogicallyCombinable<ISignal> logicExpression) : CombinationalBehavior
{
    /// <summary>
    /// The logical expression that this refers to
    /// </summary>
    public ILogicallyCombinable<ISignal> LogicExpression { get; } = logicExpression;

    /// <summary>
    /// The input signals used in this behavior. Gotten from logic expression's base objects
    /// </summary>
    public override IEnumerable<ISignal> InputSignals => LogicExpression.BaseObjects;

    /// <inheritdoc/>
    public override int? Dimension => LogicExpression.BaseObjects.FirstOrDefault()?.Dimension; // Works by getting dimension from first internal signal--they should all have the same dimension

    /// <inheritdoc/>
    public override string ToVhdl(ISignal outputSignal) => $"{outputSignal} <= {LogicExpression.ToLogicString()};";
}