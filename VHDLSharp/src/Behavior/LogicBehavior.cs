using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Behaviors;

/// <summary>
/// A behavior that uses logical expressions on signals
/// </summary>
public class LogicBehavior : Behavior, ICombinationalBehavior
{

    /// <summary>
    /// Main constructor given logical expression
    /// </summary>
    /// <param name="logicExpression">The logical expression that this refers to, as a <see cref="LogicExpression"/> or <see cref="ILogicallyCombinable{ISignal}"/></param>
    public LogicBehavior(ILogicallyCombinable<ISignal> logicExpression)
    {
        LogicExpression = LogicExpression.ToLogicExpression(logicExpression);
        ManageNewSignals(logicExpression.BaseObjects);
    }

    /// <summary>
    /// The logical expression that this refers to, as a <see cref="LogicExpression"/>
    /// </summary>
    public LogicExpression LogicExpression { get; }

    /// <summary>
    /// The module-specific input signals used in this behavior. Gotten from logic expression's base objects
    /// </summary>
    public override IEnumerable<IModuleSpecificSignal> InputModuleSignals => LogicExpression.BaseObjects.OfType<IModuleSpecificSignal>().Distinct();

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
        $"{outputSignal.GetVhdlName()} <= {LogicExpression.GetVhdl()};";

    /// <inheritdoc/>
    protected override int GetOutputValueWithoutCheck(RuleBasedSimulationState state, SignalReference outputSignal) =>
        LogicExpression.GetOutputValue(state, outputSignal.Submodule);
}