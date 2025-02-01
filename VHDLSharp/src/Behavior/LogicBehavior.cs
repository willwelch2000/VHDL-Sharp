
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;

namespace VHDLSharp.Behaviors;

/// <summary>
/// A behavior that uses logical expressions on signals
/// </summary>
/// <param name="logicExpression">The logical expression that this refers to, as a <see cref="LogicExpression"/> or <see cref="ILogicallyCombinable{ISignal}"/></param>
/// <exception cref="Exception"></exception>
public class LogicBehavior(ILogicallyCombinable<ISignal> logicExpression) : CombinationalBehavior
{
    /// <summary>
    /// The logical expression that this refers to, as a <see cref="LogicExpression"/>
    /// </summary>
    public LogicExpression LogicExpression { get; } = LogicExpression.ToLogicExpression(logicExpression);

    /// <summary>
    /// The named input signals used in this behavior. Gotten from logic expression's base objects
    /// </summary>
    public override IEnumerable<NamedSignal> NamedInputSignals => LogicExpression.BaseObjects.Where(o => o is NamedSignal).Select(o => (NamedSignal)o).Distinct();

    /// <summary>
    /// Works by getting dimension from first internal signal--they should all have the same dimension
    /// This is a definite dimension unless there are no signals added
    /// </summary>
    public override Dimension Dimension => LogicExpression.Dimension;

    /// <inheritdoc/>
    public override string ToSpice(NamedSignal outputSignal, string uniqueId)
    {
        // Don't call IsCompatible here since it does it in LogicExpression
        try
        {
            return LogicExpression.ToSpice(outputSignal, uniqueId);
        }
        catch (Exception)
        {
            throw new Exception("Output signal is not compatible with this behavior");
        }
    }

    /// <inheritdoc/>
    public override string ToVhdl(NamedSignal outputSignal)
    {
        if (!IsCompatible(outputSignal))
            throw new Exception("Output signal is not compatible with this behavior");
        return $"{outputSignal} <= {LogicExpression.ToLogicString()};";
    }

    /// <inheritdoc/>
    public override IEnumerable<IEntity> GetSpiceSharpEntities(NamedSignal outputSignal, string uniqueId)
    {
        // Don't call IsCompatible here since it does it in LogicExpression
        try
        {
            return LogicExpression.GetSpiceSharpEntities(outputSignal, uniqueId);
        }
        catch (Exception)
        {
            throw new Exception("Output signal is not compatible with this behavior");
        }
    }
}