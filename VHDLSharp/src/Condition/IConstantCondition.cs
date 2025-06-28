using SpiceSharp.Components;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;

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

    private static CustomLogicObjectOptions<ICondition, ConditionSpiceSharpObjectInput, SpiceCircuit>? conditionSpiceSharpObjectOptions;

    internal static CustomLogicObjectOptions<ICondition, ConditionSpiceSharpObjectInput, SpiceCircuit> ConditionSpiceSharpObjectOptions
    {
        get
        {
            if (conditionSpiceSharpObjectOptions is not null)
                return conditionSpiceSharpObjectOptions;

            CustomLogicObjectOptions<ICondition, ConditionSpiceSharpObjectInput, SpiceCircuit> options = new();

            SpiceCircuit AndOrFunction(IEnumerable<ILogicallyCombinable<ICondition>> innerExpressions, ConditionSpiceSharpObjectInput additionalInput, bool and)
            {
                if (!innerExpressions.Any())
                    throw new Exception($"Must be at least 1 inner expression for {(and ? "And" : "Or")} Function");

                List<SpiceCircuit> circuits = [];
                List<string> innerOutputs = [];
                foreach ((int i, ILogicallyCombinable<ICondition> innerExpression) in innerExpressions.Index())
                {
                    string subUniqueId = additionalInput.UniqueId + "_" + i;
                    string innerOutput = SpiceUtil.GetSpiceName(subUniqueId, 0, "out");
                    innerOutputs.Add(innerOutput);
                    circuits.Add(innerExpression.GenerateLogicalObject(options, new()
                    {
                        UniqueId = subUniqueId,
                        OutputSignal = new Signal(innerOutput, additionalInput.OutputSignal.ParentModule),
                    }));
                }

                circuits.Add(new([new Subcircuit(SpiceUtil.GetSpiceName(additionalInput.UniqueId, 0, and ? "And" : "Or"), and ? SpiceUtil.GetAndSubcircuit(innerOutputs.Count) : SpiceUtil.GetOrSubcircuit(innerOutputs.Count), 
                    [.. innerOutputs, additionalInput.OutputSignal.GetSpiceName()])]));
                
                return SpiceCircuit.Combine(circuits);
            }

            SpiceCircuit AndFunction(IEnumerable<ILogicallyCombinable<ICondition>> innerExpressions, ConditionSpiceSharpObjectInput additionalInput) =>
                AndOrFunction(innerExpressions, additionalInput, true);

            SpiceCircuit OrFunction(IEnumerable<ILogicallyCombinable<ICondition>> innerExpressions, ConditionSpiceSharpObjectInput additionalInput) =>
                AndOrFunction(innerExpressions, additionalInput, false);

            SpiceCircuit NotFunction(ILogicallyCombinable<ICondition> innerExpression, ConditionSpiceSharpObjectInput additionalInput)
            {
                string subUniqueId = additionalInput.UniqueId + "_0";
                string innerOutput = SpiceUtil.GetSpiceName(subUniqueId, 0, "out");
                SpiceCircuit innerCircuit = innerExpression.GenerateLogicalObject(options, new()
                {
                    UniqueId = subUniqueId,
                    OutputSignal = new Signal(innerOutput, additionalInput.OutputSignal.ParentModule)
                });

                SpiceCircuit newCircuit = new([new Subcircuit(SpiceUtil.GetSpiceName(additionalInput.UniqueId, 0, "not"), SpiceUtil.GetNotSubcircuit(),
                    innerOutput, additionalInput.OutputSignal.GetSpiceName())]);
                return innerCircuit.CombineWith(newCircuit);
            }

            SpiceCircuit BaseFunction(ICondition innerExpression, ConditionSpiceSharpObjectInput additionalInput)
            {
                if (innerExpression is not IConstantCondition constantCondition)
                    throw new Exception("Must be a constant condition to combine with others to create Spice");

                return constantCondition.GetSpice(additionalInput.UniqueId, additionalInput.OutputSignal);

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