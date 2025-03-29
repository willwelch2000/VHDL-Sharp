using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Validation;

namespace VHDLSharp.Behaviors;

/// <summary>
/// A behavior that uses logical expressions on signals
/// </summary>
/// <param name="logicExpression">The logical expression that this refers to, as a <see cref="LogicExpression"/> or <see cref="ILogicallyCombinable{ISignal}"/></param>
/// <exception cref="Exception"></exception>
public class LogicBehavior(ILogicallyCombinable<ISignal> logicExpression) : Behavior, ICombinationalBehavior
{
    /// <summary>
    /// The logical expression that this refers to, as a <see cref="LogicExpression"/>
    /// </summary>
    public LogicExpression LogicExpression { get; } = LogicExpression.ToLogicExpression(logicExpression);

    /// <summary>
    /// The named input signals used in this behavior. Gotten from logic expression's base objects
    /// </summary>
    public override IEnumerable<INamedSignal> NamedInputSignals => LogicExpression.BaseObjects.OfType<INamedSignal>().Distinct();

    /// <summary>
    /// Works by getting dimension from first internal signal--they should all have the same dimension
    /// This is a definite dimension unless there are no signals added
    /// </summary>
    public override Dimension Dimension => LogicExpression.Dimension;

    /// <inheritdoc/>
    protected override SpiceCircuit GetSpiceWithoutCheck(INamedSignal outputSignal, string uniqueId)
    {
        // Don't call IsCompatible here since it does it in LogicExpression
        try
        {
            return LogicExpression.GetSpice(outputSignal, uniqueId);
        }
        catch (IncompatibleSignalException)
        {
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        }
    }

    /// <inheritdoc/>
    protected override string GetVhdlStatementWithoutCheck(INamedSignal outputSignal)
    {
        return $"{outputSignal} <= {LogicExpression.GetVhdl()};";
    }

    /// <inheritdoc/>
    protected override SimulationRule GetSimulationRuleWithoutCheck(SignalReference outputSignal)
    {
        // Don't call IsCompatible here since it does it in LogicExpression
        try
        {
            return LogicExpression.GetSimulationRule(outputSignal);
        }
        catch (IncompatibleSignalException)
        {
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        }
    }
}