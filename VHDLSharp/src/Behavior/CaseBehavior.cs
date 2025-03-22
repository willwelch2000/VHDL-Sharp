using System.Diagnostics.CodeAnalysis;
using System.Text;
using VHDLSharp.Dimensions;
using VHDLSharp.Exceptions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;
using VHDLSharp.Validation;

namespace VHDLSharp.Behaviors;

/// <summary>
/// A behavior where an output signal is set based on a selector signal's value
/// </summary>
/// <param name="selector"></param>
public class CaseBehavior(INamedSignal selector) : Behavior, ICombinationalBehavior
{
    private readonly LogicExpression?[] caseExpressions = new LogicExpression[1 << selector.Dimension.NonNullValue];

    private LogicExpression? defaultExpression;

    /// <summary>
    /// Selector signal
    /// </summary>
    public INamedSignal Selector { get; } = selector;

    /// <summary>
    /// Expression for default case
    /// </summary>
    public LogicExpression? DefaultExpression
    {
        get => defaultExpression;
        set => SetDefault(value);
    }

    /// <inheritdoc/>
    public override IEnumerable<INamedSignal> NamedInputSignals => caseExpressions.Where(c => c is not null).SelectMany(c => c?.BaseObjects.Where(o => o is INamedSignal) ?? []).Select(o => (INamedSignal)o).Append(Selector).Distinct();

    /// <summary>
    /// Since signals have definite dimensions, the first non-null expression can be used
    /// This is either null or definite
    /// </summary>
    public override Dimension Dimension => caseExpressions.Append(DefaultExpression).FirstOrDefault(c => c is not null)?.Dimension ?? new Dimension();

    /// <inheritdoc/>
    public override string GetVhdlStatement(INamedSignal outputSignal)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal must be compatible with this behavior");
        if (!ValidityManager.IsValid())
            throw new InvalidException("Case behavior must be valid to convert to VHDL");
        if (!ValidityManager.IsValid())
            throw new InvalidException("Case behavior must be valid to convert to VHDL");
        if (!IsComplete())
            throw new IncompleteException("Case behavior must be complete to convert to VHDL");

        StringBuilder sb = new();
        sb.AppendLine($"process({string.Join(", ", NamedInputSignals.Select(s => s.Name))}) is");
        sb.AppendLine("begin");
        sb.AppendLine($"\tcase {Selector.Name} is");

        // Cases
        for (int i = 0; i < caseExpressions.Length; i++)
        {
            LogicExpression? expression = caseExpressions[i];
            if (expression is null)
                continue;
            sb.AppendLine($"\t\twhen \"{i.ToBinaryString(Selector.Dimension.NonNullValue)}\" =>");
            sb.AppendLine($"\t\t\t{outputSignal} <= {expression.GetVhdl()};");
            sb.AppendLine($"\t\t\t{outputSignal} <= {expression.GetVhdl()};");
        }

        // Default
        if (defaultExpression is not null)
        {
            sb.AppendLine($"\t\twhen others =>");
            sb.AppendLine($"\t\t\t{outputSignal} <= {defaultExpression.GetVhdl()};");
            sb.AppendLine($"\t\t\t{outputSignal} <= {defaultExpression.GetVhdl()};");
        }

        sb.AppendLine("\tend case;");
        sb.AppendLine("end process;");

        return sb.ToString();
    }

    /// <summary>
    /// Indexer for the logic expression of each case
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LogicExpression? this[int index]
    {
        get
        {
            if (index < 0 || index >= caseExpressions.Length)
                throw new ArgumentException($"Case value must be between 0 and {caseExpressions.Length-1}");
            return caseExpressions[index];
        }
        set => AddCase(index, value);
    }

    /// <summary>
    /// Indexer for the logic expressions given a boolean case
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public LogicExpression? this[bool index]
    {
        get
        {
            if (Selector.Dimension.NonNullValue != 1)
                throw new Exception("Selector dimension must be 1 for boolean value");
            return this[index ? 1 : 0];
        }
        set
        {
            if (Selector.Dimension.NonNullValue != 1)
                throw new Exception("Selector dimension must be 1 for boolean value");
            this[index ? 1 : 0] = value;
        }
    }
    
    /// <inheritdoc/>
    protected override bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        // Check parent modules
        if (base.CheckTopLevelValidity(out exception))
            return true;

        // Combine non-null dimensions of individual expressions
        IEnumerable<DefiniteDimension> dimensions = caseExpressions.Append(DefaultExpression).Where(c => c?.Dimension is not null).Select(c => c?.Dimension)!;
        // Check that dimensions of all behaviors are compatible
        if (!Dimension.AreCompatible(dimensions))
            exception = new Exception("Expressions are incompatible. Must have same or compatible dimensions");

        return exception is null;
    }

    /// <summary>
    /// Add a logic expression for a case
    /// </summary>
    /// <param name="value">integer value for selector</param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public void AddCase(int value, ILogicallyCombinable<ISignal>? logicExpression)
    {
        if (value < 0 || value >= caseExpressions.Length)
            throw new Exception($"Case value must be between 0 and {caseExpressions.Length-1}");
        CheckCompatibleNewExpression(logicExpression);
        caseExpressions[value] = logicExpression is null ? null : LogicExpression.ToLogicExpression(logicExpression);
        InvokeBehaviorUpdated(this, EventArgs.Empty);
    }

    /// <summary>
    /// Add a logic expression for a case
    /// Selector must have dimension of 1
    /// </summary>
    /// <param name="value">boolean value for selector</param>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    public void AddCase(bool value, ILogicallyCombinable<ISignal>? logicExpression)
    {
        if (Selector.Dimension.NonNullValue != 1)
            throw new Exception("Selector dimension must be 1 for boolean value");
        AddCase(value ? 1 : 0, logicExpression);
    }

    /// <summary>
    /// Set expression for default case
    /// </summary>
    /// <param name="logicExpression"></param>
    public void SetDefault(ILogicallyCombinable<ISignal>? logicExpression)
    {
        CheckCompatibleNewExpression(logicExpression);
        defaultExpression = logicExpression is null ? null : LogicExpression.ToLogicExpression(logicExpression);
    }

    /// <summary>
    /// Is the behavior ready to be used
    /// </summary>
    /// <returns></returns>
    public bool IsComplete() => caseExpressions.All(c => c is not null) || defaultExpression is not null;

    /// <summary>
    /// Checks if new (nullable) logic expression is compatible with the current state, given the expressions that have already been assigned
    /// </summary>
    /// <param name="logicExpression"></param>
    /// <exception cref="Exception"></exception>
    private void CheckCompatibleNewExpression(ILogicallyCombinable<ISignal>? logicExpression)
    {
        // Fine if new one is null
        if (logicExpression is null)
            return;

        foreach (LogicExpression? expression in caseExpressions.Append(DefaultExpression))
        {
            // Check ability to combine in both directions
            if (expression is not null && !(expression.CanCombine(logicExpression) && logicExpression.CanCombine(expression)))
                throw new IncompatibleSignalException($"Given expression is incompatible with pre-existing expression (must have parent {ParentModule} and dimension must be {Dimension?.ToString() ?? "N/A"})");
        }
    }

    /// <inheritdoc/>
    public override SpiceCircuit GetSpice(INamedSignal outputSignal, string uniqueId)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal must be compatible with this behavior");
        if (!ValidityManager.IsValid())
            throw new InvalidException("Case behavior must be valid to convert to Spice circuit");
        if (!IsComplete())
            throw new IncompleteException("Case behavior must be complete to convert to Spice circuit");

        return SpiceCircuit.Combine(ToLogicBehaviors(outputSignal, uniqueId).Select(behaviorObj => behaviorObj.behavior.GetSpice(behaviorObj.outputSignal, behaviorObj.uniqueId))).WithCommonEntities();
    }

    private IEnumerable<(INamedSignal outputSignal, string uniqueId, LogicBehavior behavior)> ToLogicBehaviors(INamedSignal outputSignal, string uniqueId)
    {
        // Loop through cases, generating intermediate signal names and logic behaviors to map to them
        INamedSignal[] caseIntermediateSignals = new INamedSignal[caseExpressions.Length];
        int idCounter = 0;
        for (int i = 0; i < caseExpressions.Length; i++)
        {
            // Expression comes from case or default
            LogicExpression expression = caseExpressions[i] ?? DefaultExpression ?? throw new("Impossible");

            // Generate intermediate signal matching dimension of output
            string intermediateName = SpiceUtil.GetSpiceName(uniqueId, 0, $"case{i}");
            INamedSignal signal;
            if (expression.Dimension.NonNullValue == 1)
                signal = new Signal(intermediateName, outputSignal.ParentModule);
            else
                signal = new Vector(intermediateName, outputSignal.ParentModule, outputSignal.Dimension.NonNullValue);
            caseIntermediateSignals[i] = signal;
            
            yield return (signal, $"{uniqueId}_{idCounter++}", new LogicBehavior(expression));
        }

        // The individual nodes of the selector signal and output signal
        ISingleNodeNamedSignal[] selectorSingleNodeSignals = [.. Selector.ToSingleNodeSignals];
        ISingleNodeNamedSignal[] outputSignalSingleNodes = [.. outputSignal.ToSingleNodeSignals];

        // Here, do MUX for each dimension
        for (int dim = 0; dim < outputSignal.Dimension.NonNullValue; dim++)
        {
            // Array for each AND in the MUX--these will be ORed at the end
            And<ISignal>[] selectedExpressions = new And<ISignal>[caseExpressions.Length];

            // Loop through case expressions to formulate selectedExpressions
            for (int i = 0; i < selectedExpressions.Length; i++)
            {
                // What each bit of the selector needs to be for this expression to be used
                IEnumerable<bool> selectorValues = Enumerable.Range(0, Selector.Dimension.NonNullValue).Select(j => (i & 1<<j) > 0);
                
                // Convert selector values into actual signals--either the node of the selector or the NOT of that
                List<ILogicallyCombinable<ISignal>> selectorExpressions = [];
                foreach ((bool selectorValue, int index) in selectorValues.Select((val, index) => (val, index)))
                {
                    selectorExpressions.Add(
                        selectorValue ? selectorSingleNodeSignals[index] :
                        new Not<ISignal>(selectorSingleNodeSignals[index])
                    );
                }

                ISingleNodeNamedSignal singleNodeSignal = caseIntermediateSignals[i].ToSingleNodeSignals.ElementAt(dim);
                selectedExpressions[i] = new And<ISignal>([.. selectorExpressions, singleNodeSignal]);
            }
            
            // OR all the selected expressions together to complete MUX
            LogicBehavior behavior = new(new LogicExpression(new Or<ISignal>(selectedExpressions)));
            yield return (outputSignalSingleNodes[dim], $"{uniqueId}_{idCounter++}", behavior);
        }
    }
}