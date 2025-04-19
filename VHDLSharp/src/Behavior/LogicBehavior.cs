using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;

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
    protected override SpiceCircuit GetSpiceWithoutCheck(INamedSignal outputSignal, string uniqueId) => 
        LogicExpression.GetSpice(outputSignal, uniqueId);

    /// <inheritdoc/>
    protected override string GetVhdlStatementWithoutCheck(INamedSignal outputSignal) =>
        $"{outputSignal} <= {LogicExpression.GetVhdl()};";

    /// <inheritdoc/>
    protected override SimulationRule GetSimulationRuleWithoutCheck(SignalReference outputSignal) =>
        new(outputSignal, (state) => LogicExpression.GetOutputValue(state, outputSignal.Subcircuit));
}