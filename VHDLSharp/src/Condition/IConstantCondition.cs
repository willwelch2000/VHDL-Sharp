using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Conditions;

/// <summary>
/// A <see cref="ICondition"/> that can be true for extended periods of time. 
/// For example, an equality comparison
/// </summary>
public interface IConstantCondition : ICondition
{
    /// <summary>
    /// Get a <see cref="SpiceCircuit"/> that produces an output signal 
    /// corresponding to the boolean value of the condition. The output signal
    /// should be high whenever the condition is true. 
    /// </summary>
    /// <param name="uniqueId"></param>
    /// <param name="outputSignal"></param>
    /// <returns></returns>
    public SpiceCircuit GetSpice(string uniqueId, ISingleNodeNamedSignal outputSignal);

    private static CustomLogicObjectOptions<ICondition, ConditionSpiceSharpObjectInput, ConditionSpiceSharpObjectOutput>? conditionSpiceSharpObjectOptions;

    internal static CustomLogicObjectOptions<ICondition, ConditionSpiceSharpObjectInput, ConditionSpiceSharpObjectOutput> ConditionSpiceSharpObjectOptions
    {
        get
        {
            if (conditionSpiceSharpObjectOptions is not null)
                return conditionSpiceSharpObjectOptions;

            CustomLogicObjectOptions<ICondition, ConditionSpiceSharpObjectInput, ConditionSpiceSharpObjectOutput> options = new();

            ConditionSpiceSharpObjectOutput AndFunction(IEnumerable<ILogicallyCombinable<ICondition>> innerExpressions, ConditionSpiceSharpObjectInput additionalInput)
            {
                if (!innerExpressions.Any())
                    throw new Exception("Must be at least 1 inner expression for And Function");
            }
            
            ConditionSpiceSharpObjectOutput OrFunction(IEnumerable<ILogicallyCombinable<ICondition>> innerExpressions, ConditionSpiceSharpObjectInput additionalInput)
            {
                if (!innerExpressions.Any())
                    throw new Exception("Must be at least 1 inner expression for Or Function");
            }

            ConditionSpiceSharpObjectOutput NotFunction(ILogicallyCombinable<ICondition> innerExpression, ConditionSpiceSharpObjectInput additionalInput)
            {

            }

            ConditionSpiceSharpObjectOutput BaseFunction(ICondition innerExpression, ConditionSpiceSharpObjectInput additionalInput)
            {
                if (innerExpression is not IConstantCondition constantCondition)
                    throw new Exception("Must be a constant condition to combine with others to create Spice");

                return new()
                {
                    SpiceSharpEntities = constantCondition.GetSpice(additionalInput.UniqueId, additionalInput.OutputSignal).CircuitElements,
                };

            }

            options.AndFunction = AndFunction;
            options.OrFunction = OrFunction;
            options.NotFunction = NotFunction;
            options.BaseFunction = BaseFunction;

            conditionSpiceSharpObjectOptions = options;
            return options;
        }
    }
}