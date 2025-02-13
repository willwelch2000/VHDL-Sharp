
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
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
    public override IEnumerable<INamedSignal> NamedInputSignals => LogicExpression.BaseObjects.Where(o => o is INamedSignal).Select(o => (INamedSignal)o).Distinct();

    /// <summary>
    /// Works by getting dimension from first internal signal--they should all have the same dimension
    /// This is a definite dimension unless there are no signals added
    /// </summary>
    public override Dimension Dimension => LogicExpression.Dimension;

    /// <inheritdoc/>
    public override string ToSpice(INamedSignal outputSignal, string uniqueId)
    {
        // Don't call IsCompatible here since it does it in LogicExpression
        try
        {
            return LogicExpression.ToSpice(outputSignal, uniqueId);
        }
        catch (IncompatibleSignalException)
        {
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        }
    }

    /// <inheritdoc/>
    public override string ToVhdl(INamedSignal outputSignal)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return $"{outputSignal} <= {LogicExpression.ToLogicString()};";
    }

    /// <inheritdoc/>
    public override IEnumerable<IEntity> GetSpiceSharpEntities(INamedSignal outputSignal, string uniqueId)
    {
        // Don't call IsCompatible here since it does it in LogicExpression
        try
        {
            return LogicExpression.GetSpiceSharpEntities(outputSignal, uniqueId);
        }
        catch (IncompatibleSignalException)
        {
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        }
    }
}