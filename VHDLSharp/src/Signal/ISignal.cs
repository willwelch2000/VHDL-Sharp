using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Simulations;
using VHDLSharp.Utility;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any type of signal that can be used in an expression.
/// It is assumed that parent-child relationships and the dimension are not changed after construction.
/// An implementation that breaks this rule could cause validation issues.
/// Classes should not directly implement this. 
/// Instead, they should implement <see cref="INamedSignal"/>, <see cref="ISignalWithKnownValue"/>, <see cref="IDerivedSignal"/>, or <see cref="IDerivedSignalNode"/>, which extend this.
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal>
{
    /// <summary>
    /// Object explaining how many nodes are part of this signal (1 for normal signal)
    /// </summary> 
    public DefiniteDimension Dimension { get; }

    /// <summary>
    /// Indexer for multi-dimensional signals. 
    /// A single-dimensional signal will just return itself for the first item
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ISingleNodeSignal this[int index] { get; }

    /// <summary>
    /// If this is part of a larger group (e.g. vector node), get the parent signal (one layer up)
    /// </summary>
    public ISignal? ParentSignal { get; }

    /// <summary>
    /// If this is the top level, it returns this. 
    /// Otherwise, it goes up in hierarchy as much as possible
    /// </summary>
    public ISignal TopLevelSignal
    {
        get
        {
            ISignal signal = this;
            while (signal.ParentSignal is not null)
                signal = signal.ParentSignal;
            return signal;
        }
    }

    /// <summary>
    /// If this has children (e.g. vector), get the child signals
    /// </summary>
    public IEnumerable<ISignal> ChildSignals { get; }

    /// <summary>
    /// If this has a dimension > 1, convert to a list of things with dimension 1. 
    /// If it is dimension 1, then return itself.
    /// The Spice and Spice# objects use these to find the Spice names
    /// </summary>
    public IEnumerable<ISingleNodeSignal> ToSingleNodeSignals
    {
        get
        {
            if (ChildSignals.Any())
                return ChildSignals.SelectMany(s => s.ToSingleNodeSignals);
            return this is ISingleNodeSignal singleNodeSignal ? [singleNodeSignal] : [];
        }
    }

    /// <summary>
    /// Get name for use in VHDL module
    /// </summary>
    /// <returns></returns>
    public string GetVhdlName();

    /// <summary>
    /// Get value of the signal given rule-based simulation state and context
    /// </summary>
    /// <param name="state"></param>
    /// <param name="context"></param>
    /// <param name="lastIndex"></param>
    /// <returns>Value if possible</returns>
    /// <exception cref="Exception">If signal doesn't implement <see cref="INamedSignal"/>, <see cref="ISignalWithKnownValue"/>, <see cref="IDerivedSignal"/>, or <see cref="IDerivedSignalNode"/>
    /// or if it doesn't have a value in the state yet</exception>
    internal int GetLastOutputValue(RuleBasedSimulationState state, SubcircuitReference context, int? lastIndex = null)
    {
        switch (this)
        {
            case ISignalWithKnownValue signalWithKnownValue:
                return signalWithKnownValue.Value;
            case INamedSignal or IDerivedSignal or IDerivedSignalNode:
                // Get the signal itself or the linked signal, if derived
                INamedSignal signalToUse = this switch
                {
                    INamedSignal namedSignal => namedSignal,
                    IDerivedSignal derivedSignal => derivedSignal.LinkedSignal ?? throw new Exception("No linked signal on derived signal"),
                    IDerivedSignalNode derivedSignalNode => derivedSignalNode.LinkedSignal ?? throw new Exception("No linked signal on derived signal node"),
                    _ => throw new("Impossible"),
                };
                // Find most-recent value of the signal
                return state.GetSignalValues(context.GetChildSignalReference(signalToUse)) switch
                {
                    List<int> values when values.Count > (lastIndex ?? state.CurrentTimeStepIndex - 1) => values[lastIndex ?? state.CurrentTimeStepIndex - 1],
                    _ => throw new Exception("Values list not long enough"),
                };
            default:
                throw new Exception("Signals used must extend either INamedSignal or ISignalWithKnownValue");
        }
    }

    /// <summary>
    /// Given several signals, returns true if they can be combined together
    /// </summary>
    /// <param name="combinables"></param>
    /// <returns></returns>
    internal static bool CanCombineSignals(IEnumerable<ILogicallyCombinable<ISignal>> combinables)
    {
        IEnumerable<ISignal> baseSignals = combinables.SelectMany(c => c.BaseObjects);

        // 1 or 0 signals is always true
        if (baseSignals.Count() < 2)
            return true;

        // Find signal with assigned module, if it exists
        IModuleSpecificSignal? namedSignal = baseSignals.FirstOrDefault(s => s is IModuleSpecificSignal) as IModuleSpecificSignal;
        if (namedSignal is not null)
        {
            // If any signal has another parent
            if (baseSignals.Any(s => s is IModuleSpecificSignal namedS && !namedS.ParentModule.Equals(namedSignal.ParentModule)))
                return false;
        }

        // If any signal has incompatible dimension with first
        ISignal first = baseSignals.First();
        if (baseSignals.Any(s => !s.Dimension.Compatible(first.Dimension)))
            return false;

        return true;
    }

    internal static bool CanCombineSignals(IModuleSpecificSignal signalWithModule, ILogicallyCombinable<ISignal> other)
    {
        // If there's a signal with a parent module, check that one--otherwise, get the first available
        ISignal? signal = other.BaseObjects.FirstOrDefault(e => e is IModuleSpecificSignal) ?? other.BaseObjects.FirstOrDefault();
        if (signal is null)
            return true;
        // Fine if dimension is compatible and parent is nonexistent or compatible
        return signalWithModule.Dimension.Compatible(signal.Dimension) && (signal is not IModuleSpecificSignal namedSignal || signalWithModule.ParentModule.Equals(namedSignal.ParentModule));
    }

    private static CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput>? signalSpiceSharpObjectOptions;
    private static CustomLogicObjectOptions<ISignal, SignalVhdlObjectInput, SignalVhdlObjectOutput>? signalVhdlObjectOptions;

    internal static CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput> SignalSpiceSharpObjectOptions
    {
        get
        {
            if (signalSpiceSharpObjectOptions is not null)
                return signalSpiceSharpObjectOptions;

            CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput> options = new();

            SignalSpiceSharpObjectOutput AndFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalSpiceSharpObjectInput additionalInput)
            {
                if (!innerExpressions.Any())
                    throw new Exception("Must be at least 1 inner expression for And Function");

                string uniqueId = additionalInput.UniqueId;
                int dimension = 0; // Set first time through loop
                List<IEntity> entities = [];
                // Each entry is an array of 1-per-dimension signals from an inner expression--in a 1-d signal, each entry would just have 1 string
                List<string[]> inputSignalNames = [];

                // Loop through each inner expression--these each correspond to a PMOS and NMOS per dimension
                foreach ((ILogicallyCombinable<ISignal> innerExpression, int i) in innerExpressions.Select((item, i) => (item, i)))
                {
                    // Run function for inner expression to get gate input signal
                    string innerUniqueId = uniqueId + $"_{i}";
                    SignalSpiceSharpObjectOutput innerOutput = innerExpression.GenerateLogicalObject(options, new()
                    {
                        UniqueId = innerUniqueId,
                    });

                    // Add inner entities
                    entities.AddRange(innerOutput.SpiceSharpEntities);

                    // Set dimension first time through
                    if (i == 0)
                        dimension = innerOutput.Dimension;

                    // Add inner output signal names to list
                    inputSignalNames.Add(innerOutput.OutputSignalNames);
                }

                // Generate output signal names (1 per dimension)
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => SpiceUtil.GetSpiceName(uniqueId, i, "andout"))];

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    Subcircuit andSubcircuit = new(SpiceUtil.GetSpiceName(uniqueId, i, "and"), SpiceUtil.GetAndSubcircuit(inputSignalNames.Count),
                        [.. inputSignalNames.Select(s => s[i]), outputSignalNames[i]]);
                    entities.Add(andSubcircuit);
                }

                return new()
                {
                    SpiceSharpEntities = entities,
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                };
            }

            SignalSpiceSharpObjectOutput OrFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalSpiceSharpObjectInput additionalInput)
            {
                if (!innerExpressions.Any())
                    throw new Exception("Must be at least 1 inner expression for Or Function");

                string uniqueId = additionalInput.UniqueId;
                int dimension = 0; // Set first time through loop
                List<IEntity> entities = [];
                // Each entry is an array of 1-per-dimension signals from an inner expression--in a 1-d signal, each entry would just have 1 string
                List<string[]> inputSignalNames = [];

                // Loop through each inner expression--these each correspond to a PMOS and NMOS per dimension
                foreach ((ILogicallyCombinable<ISignal> innerExpression, int i) in innerExpressions.Select((item, i) => (item, i)))
                {
                    // Run function for inner expression to get gate input signal
                    string innerUniqueId = uniqueId + $"_{i}";
                    SignalSpiceSharpObjectOutput innerOutput = innerExpression.GenerateLogicalObject(options, new()
                    {
                        UniqueId = innerUniqueId,
                    });

                    // Add inner entities
                    entities.AddRange(innerOutput.SpiceSharpEntities);

                    // Set dimension first time through
                    if (i == 0)
                        dimension = innerOutput.Dimension;

                    // Add inner output signal names to list
                    inputSignalNames.Add(innerOutput.OutputSignalNames);
                }

                // Generate output signal names (1 per dimension)
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => SpiceUtil.GetSpiceName(uniqueId, i, "orout"))];

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    Subcircuit orSubcircuit = new(SpiceUtil.GetSpiceName(uniqueId, i, "or"), SpiceUtil.GetOrSubcircuit(inputSignalNames.Count),
                        [.. inputSignalNames.Select(s => s[i]), outputSignalNames[i]]);
                    entities.Add(orSubcircuit);
                }

                return new()
                {
                    SpiceSharpEntities = entities,
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                };
            }

            SignalSpiceSharpObjectOutput NotFunction(ILogicallyCombinable<ISignal> innerExpression, SignalSpiceSharpObjectInput additionalInput)
            {
                string uniqueId = additionalInput.UniqueId;

                // Run function for inner expression to get gate input signals (1 per dimension)
                string innerUniqueId = uniqueId + "_0";
                SignalSpiceSharpObjectOutput innerOutput = innerExpression.GenerateLogicalObject(options, new()
                {
                    UniqueId = innerUniqueId,
                });
                int dimension = innerOutput.Dimension; // Get dimension from inner

                // Generate output signal names (1 per dimension)
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => SpiceUtil.GetSpiceName(uniqueId, i, "notout"))];

                // For each dimension, add PMOS and NMOS to form NOT gate going from inner output signal name to output signal name
                List<IEntity> entities = [.. innerOutput.SpiceSharpEntities];
                for (int i = 0; i < dimension; i++)
                {
                    Subcircuit notSubcircuit = new(SpiceUtil.GetSpiceName(uniqueId, i, "or"), SpiceUtil.GetNotSubcircuit(),
                        innerOutput.OutputSignalNames[i], outputSignalNames[i]);
                    entities.Add(notSubcircuit);
                }

                return new()
                {
                    SpiceSharpEntities = entities,
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                };
            }

            SignalSpiceSharpObjectOutput BaseFunction(ISignal innerExpression, SignalSpiceSharpObjectInput additionalInput)
            {
                // Get signals as strings and generate output signal names
                string[] signals = [.. innerExpression.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
                string uniqueId = additionalInput.UniqueId;
                string[] outputSignals = [.. Enumerable.Range(0, signals.Length).Select(i => SpiceUtil.GetSpiceName(uniqueId, i, "baseout"))];

                // Add 1m resistors between signals and output signals
                IEnumerable<IEntity> entities = Enumerable.Range(0, signals.Length).Select(i =>
                    new Resistor(SpiceUtil.GetSpiceName(uniqueId, i, "res"), signals[i], outputSignals[i], 1e-3));

                return new()
                {
                    SpiceSharpEntities = entities,
                    Dimension = signals.Length,
                    OutputSignalNames = outputSignals,
                };
            }

            options.AndFunction = AndFunction;
            options.OrFunction = OrFunction;
            options.NotFunction = NotFunction;
            options.BaseFunction = BaseFunction;

            signalSpiceSharpObjectOptions = options;
            return options;
        }
    }

    internal static CustomLogicObjectOptions<ISignal, SignalVhdlObjectInput, SignalVhdlObjectOutput> SignalVhdlObjectOptions
    {
        get
        {
            if (signalVhdlObjectOptions is not null)
                return signalVhdlObjectOptions;

            CustomLogicObjectOptions<ISignal, SignalVhdlObjectInput, SignalVhdlObjectOutput> options = new();

            SignalVhdlObjectOutput AndFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalVhdlObjectInput additionalInput)
            {
                IEnumerable<SignalVhdlObjectOutput> innerOutputs = innerExpressions.Select(i => i.GenerateLogicalObject(options, new()));
                return new()
                {
                    VhdlString = "(" + string.Join(" and ", innerOutputs.Select(i => i.VhdlString)) + ")"
                };
            }

            SignalVhdlObjectOutput OrFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalVhdlObjectInput additionalInput)
            {
                IEnumerable<SignalVhdlObjectOutput> innerOutputs = innerExpressions.Select(i => i.GenerateLogicalObject(options, new()));
                return new()
                {
                    VhdlString = "(" + string.Join(" or ", innerOutputs.Select(i => i.VhdlString)) + ")"
                };
            }

            SignalVhdlObjectOutput NotFunction(ILogicallyCombinable<ISignal> innerExpression, SignalVhdlObjectInput additionalInput)
            {
                SignalVhdlObjectOutput innerOutput = innerExpression.GenerateLogicalObject(options, new());
                return new()
                {
                    VhdlString = $"(not ({innerOutput.VhdlString}))"
                };
            }

            SignalVhdlObjectOutput BaseFunction(ISignal innerExpression, SignalVhdlObjectInput additionalInput)
            {
                return new()
                {
                    VhdlString = innerExpression.GetVhdlName()
                };
            }

            options.AndFunction = AndFunction;
            options.OrFunction = OrFunction;
            options.NotFunction = NotFunction;
            options.BaseFunction = BaseFunction;

            signalVhdlObjectOptions = options;
            return options;
        }
    }
}