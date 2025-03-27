using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
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
    public override SpiceCircuit GetSpice(INamedSignal outputSignal, string uniqueId)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Logic behavior must be valid to convert to Spice circuit");
            
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
    public override string GetVhdlStatement(INamedSignal outputSignal)
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Logic behavior must be valid to convert to VHDL");
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal is not compatible with this behavior");
        return $"{outputSignal} <= {LogicExpression.GetVhdl()};";
    }
}