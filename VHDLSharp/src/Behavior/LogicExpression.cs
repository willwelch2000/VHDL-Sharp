using System.Text.RegularExpressions;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Behaviors;

/// <summary>
/// Logical expression that can be used in a <see cref="LogicBehavior"/>
/// Basically just wraps <see cref="ILogicallyCombinable{ISignal}"/>
/// </summary>
/// <param name="expression">Input expression</param>
// We don't need to check expression because it can't be created if the signals aren't compatible
public class LogicExpression(ILogicallyCombinable<ISignal> expression) : ILogicallyCombinable<ISignal>
{
    /// <summary>
    /// Inner expression that backs this logic expression
    /// </summary>
    public ILogicallyCombinable<ISignal> InnerExpression { get; } = expression;

    /// <inheritdoc/>
    public IEnumerable<ISignal> BaseObjects => InnerExpression.BaseObjects;

    /// <summary>
    /// Get dimension of this expression
    /// Works by getting dimension from first signal in expression
    /// Valid because signals have definite dimensions
    /// </summary>
    /// <returns></returns>
    public DefiniteDimension Dimension => InnerExpression.BaseObjects.First().Dimension;
    
    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other) => InnerExpression.CanCombine(other);

    /// <inheritdoc/>
    public string ToLogicString() => InnerExpression.ToLogicString();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => InnerExpression.ToLogicString(options);

    /// <inheritdoc/>
    public TOut GenerateLogicalObject<TIn, TOut>(CustomLogicObjectOptions<ISignal, TIn, TOut> options, TIn additionalInput) where TOut : new()
    {
        return InnerExpression.GenerateLogicalObject(options, additionalInput);
    }

    /// <summary>
    /// Get VHDL representation of logical expression. 
    /// Only includes the right-hand side of the VHDL statement
    /// </summary>
    /// <returns></returns>
    public string GetVhdl() => InnerExpression.GenerateLogicalObject(SignalVhdlObjectOptions, new()).VhdlString;

    /// <summary>
    /// Gets Spice representation of logical expression of signals
    /// </summary>
    /// <param name="outputSignal">Output signal for this expression</param>
    /// <param name="uniqueId">Unique string provided to this expression so that it can have a unique name</param>
    /// <returns></returns>
    public string GetSpice(INamedSignal outputSignal, string uniqueId)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal must be compatible with this expression");

        SignalSpiceObjectOutput output = InnerExpression.GenerateLogicalObject(SignalSpiceObjectOptions, new()
        {
            UniqueId = uniqueId,
        });

        ISingleNodeNamedSignal[] singleNodeSignals = [.. outputSignal.ToSingleNodeSignals];
        if (output.Dimension != singleNodeSignals.Length)
            throw new Exception("Expression dimension didn't match output signal dimension");

        // Convert final output signals from output into correct output signals
        string value = output.SpiceString;
        for (int i = 0; i < output.Dimension; i++)
        {
            string newSignalName = singleNodeSignals[i].GetSpiceName();
            string oldSignalName = output.OutputSignalNames[i];

            value = Regex.Replace(value, $@"\b{oldSignalName}\b", newSignalName);
        }

        return value;
    }

    /// <summary>
    /// Get expression as list of entities for Spice#
    /// </summary>
    /// <param name="outputSignal">Output signal for this expression</param>
    /// <param name="uniqueId">Unique string provided to this expression so that it can have a unique name</param>
    public IEnumerable<IEntity> GetSpiceSharpEntities(INamedSignal outputSignal, string uniqueId)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal must be compatible with this expression");

        SignalSpiceSharpObjectOutput output = InnerExpression.GenerateLogicalObject(SignalSpiceSharpObjectOptions, new()
        {
            UniqueId = uniqueId,
        });

        List<IEntity> entities = [.. output.SpiceSharpEntities];

        ISingleNodeNamedSignal[] singleNodeSignals = [.. outputSignal.ToSingleNodeSignals];
        if (output.Dimension != singleNodeSignals.Length)
            throw new Exception("Expression dimension didn't match output signal dimension");

        // Convert final output signals from output into correct output signals
        for (int i = 0; i < output.Dimension; i++)
        {
            string newSignalName = singleNodeSignals[i].GetSpiceName();
            string oldSignalName = output.OutputSignalNames[i];
            
            // Connect oldSignalName to newSignalName via 1m resistor
            entities.Add(new Resistor("R" + Util.GetSpiceName(uniqueId, i, "connect"), oldSignalName, newSignalName, 1e-3));
        }

        return entities;
    }

    /// <summary>
    /// Generate an And with this expression and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public LogicExpression And(ILogicallyCombinable<ISignal> other) => new(new And<ISignal>(InnerExpression, other));

    /// <summary>
    /// Generate an And with this expression and other <see cref="ILogicallyCombinable{T}"/> objects
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public LogicExpression And(IEnumerable<ILogicallyCombinable<ISignal>> others) => new(new And<ISignal>([.. others, InnerExpression]));

    /// <summary>
    /// Generate an Or with this expression and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public LogicExpression Or(ILogicallyCombinable<ISignal> other) => new(new And<ISignal>(InnerExpression, other));

    /// <summary>
    /// Generate an Or with this expression and other <see cref="ILogicallyCombinable{T}"/> objects
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public LogicExpression Or(IEnumerable<ILogicallyCombinable<ISignal>> others) => new(new Or<ISignal>([.. others, InnerExpression]));

    /// <summary>
    /// Generate a Not with this expression
    /// </summary>
    /// <returns></returns>
    public LogicExpression Not() => new(new Not<ISignal>(InnerExpression));

    private static CustomLogicObjectOptions<ISignal, SignalSpiceObjectInput, SignalSpiceObjectOutput>? signalSpiceObjectOptions;

    private static CustomLogicObjectOptions<ISignal, SignalSpiceObjectInput, SignalSpiceObjectOutput> SignalSpiceObjectOptions 
    {
        get
        {
            signalSpiceObjectOptions ??= ISignal.SignalSpiceObjectOptions;
            return signalSpiceObjectOptions;
        }
    }

    private static CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput>? signalSpiceSharpObjectOptions;

    private static CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput> SignalSpiceSharpObjectOptions
    {
        get
        {
            signalSpiceSharpObjectOptions ??= ISignal.SignalSpiceSharpObjectOptions;
            return signalSpiceSharpObjectOptions!;
        }
    }

    private static CustomLogicObjectOptions<ISignal, SignalVhdlObjectInput, SignalVhdlObjectOutput>? signalVhdlObjectOptions;

    private static CustomLogicObjectOptions<ISignal, SignalVhdlObjectInput, SignalVhdlObjectOutput> SignalVhdlObjectOptions
    {
        get
        {
            signalVhdlObjectOptions ??= ISignal.SignalVhdlObjectOptions;
            return signalVhdlObjectOptions!;
        }
    }

    /// <summary>
    /// Convert a <see cref="ILogicallyCombinable{ISignal}"/> to a <see cref="LogicExpression"/>
    /// If the given argument is already of the correct type, it just returns that
    /// Otherwise, it creates a new <see cref="LogicExpression"/> that links to it
    /// </summary>
    /// <param name="expression">Input expression</param>
    /// <returns></returns>
    public static LogicExpression ToLogicExpression(ILogicallyCombinable<ISignal> expression)
        => expression is LogicExpression logicExpression ? logicExpression : new(expression);

    /// <summary>
    /// Check if a given output signal is compatible with this
    /// </summary>
    /// <param name="outputSignal"></param>
    public bool IsCompatible(INamedSignal outputSignal) => InnerExpression.CanCombine(outputSignal);
}