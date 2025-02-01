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
public class LogicExpression(ILogicallyCombinable<ISignal> expression) : ILogicallyCombinable<ISignal>
{
    private readonly ILogicallyCombinable<ISignal> expression = expression;

    /// <inheritdoc/>
    public IEnumerable<ISignal> BaseObjects => expression.BaseObjects;

    /// <summary>
    /// Get dimension of this expression
    /// Works by getting dimension from first signal in expression
    /// Valid because signals have definite dimensions
    /// </summary>
    /// <returns></returns>
    public DefiniteDimension Dimension => expression.BaseObjects.First().Dimension;
    
    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other) => expression.CanCombine(other);

    /// <inheritdoc/>
    public string ToLogicString() => expression.ToLogicString();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => expression.ToLogicString(options);

    /// <inheritdoc/>
    public TOut GenerateLogicalObject<TIn, TOut>(CustomLogicObjectOptions<ISignal, TIn, TOut> options, TIn additionalInput) where TOut : new()
    {
        return expression.GenerateLogicalObject(options, additionalInput);
    }

    /// <summary>
    /// Gets Spice representation of logical expression of signals
    /// </summary>
    /// <param name="outputSignal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public string ToSpice(NamedSignal outputSignal, string uniqueId)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal must be compatible with this expression");

        SignalSpiceObjectOutput output = expression.GenerateLogicalObject(SignalSpiceObjectOptions, new()
        {
            UniqueId = uniqueId,
        });

        SingleNodeNamedSignal[] singleNodeSignals = [.. outputSignal.ToSingleNodeSignals];
        if (output.Dimension != singleNodeSignals.Length)
            throw new Exception("Expression dimension didn't match output signal dimension");

        // Convert final output signals from output into correct output signals
        string value = output.SpiceString;
        for (int i = 0; i < output.Dimension; i++)
        {
            string newSignalName = singleNodeSignals[i].ToSpice();
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
    public IEnumerable<IEntity> GetSpiceSharpEntities(NamedSignal outputSignal, string uniqueId)
    {
        if (!IsCompatible(outputSignal))
            throw new IncompatibleSignalException("Output signal must be compatible with this expression");

        SignalSpiceSharpObjectOutput output = expression.GenerateLogicalObject(SignalSpiceSharpObjectOptions, new()
        {
            UniqueId = uniqueId,
        });

        List<IEntity> entities = [.. output.SpiceSharpEntities];

        SingleNodeNamedSignal[] singleNodeSignals = [.. outputSignal.ToSingleNodeSignals];
        if (output.Dimension != singleNodeSignals.Length)
            throw new Exception("Expression dimension didn't match output signal dimension");

        // Convert final output signals from output into correct output signals
        for (int i = 0; i < output.Dimension; i++)
        {
            string newSignalName = singleNodeSignals[i].ToSpice();
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
    public LogicExpression And(ILogicallyCombinable<ISignal> other) => new(new And<ISignal>(expression, other));

    /// <summary>
    /// Generate an And with this expression and other <see cref="ILogicallyCombinable{T}"/> objects
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public LogicExpression And(IEnumerable<ILogicallyCombinable<ISignal>> others) => new(new And<ISignal>([.. others, expression]));

    /// <summary>
    /// Generate an Or with this expression and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public LogicExpression Or(ILogicallyCombinable<ISignal> other) => new(new And<ISignal>(expression, other));

    /// <summary>
    /// Generate an Or with this expression and other <see cref="ILogicallyCombinable{T}"/> objects
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public LogicExpression Or(IEnumerable<ILogicallyCombinable<ISignal>> others) => new(new Or<ISignal>([.. others, expression]));

    /// <summary>
    /// Generate a Not with this expression
    /// </summary>
    /// <returns></returns>
    public LogicExpression Not() => new(new Not<ISignal>(expression));

    private static CustomLogicObjectOptions<ISignal, SignalSpiceObjectInput, SignalSpiceObjectOutput>? signalSpiceObjectOptions;

    private static CustomLogicObjectOptions<ISignal, SignalSpiceObjectInput, SignalSpiceObjectOutput> SignalSpiceObjectOptions 
    {
        get
        {
            if (signalSpiceObjectOptions is not null)
                return signalSpiceObjectOptions;

            CustomLogicObjectOptions<ISignal, SignalSpiceObjectInput, SignalSpiceObjectOutput> options = new();

            // In each of the following functions, the functionality is done once per dimension in parallel

            SignalSpiceObjectOutput AndFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalSpiceObjectInput additionalInput)
            {
                if (!innerExpressions.Any())
                    throw new Exception("Must be at least 1 inner expression for And Function");

                string uniqueId = additionalInput.UniqueId;
                int dimension = 0; // Set first time through loop
                string returnVal = "";
                // Each entry is an array of 1-per-dimension signals from an inner expression--in a 1-d signal, each entry would just have 1 string
                List<string[]> inputSignalNames = [];

                // Loop through each inner expression--these each correspond to a PMOS and NMOS per dimension
                foreach ((ILogicallyCombinable<ISignal> innerExpression, int i) in innerExpressions.Select((item, i) => (item, i)))
                {
                    // Run function for inner expression to get gate input signal
                    string innerUniqueId = uniqueId + $"_{i}";
                    SignalSpiceObjectOutput innerOutput = innerExpression.GenerateLogicalObject(options, new()
                    {
                        UniqueId = innerUniqueId,
                    });
                    
                    // Add inner entities
                    returnVal += innerOutput.SpiceString;

                    // Set dimension first time through
                    if (i == 0)
                        dimension = innerOutput.Dimension;

                    // Add inner output signal names to list
                    inputSignalNames.Add(innerOutput.OutputSignalNames);
                }

                // Generate nand output signal names and final output signal names (1 per dimension)
                string[] nandSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "nandout"))];
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "andout"))];

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    // Add a PMOS and NMOS for each input signal
                    foreach ((string inputSignal, int j) in inputSignalNames.Select((s, j) => (s[i], j)))
                    {
                        // PMOSs go in parallel from VDD to nandSignal
                        returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, $"pnand{j}"), nandSignalNames[i], inputSignal, "VDD", true);
                        // NMOSs go in series from nandSignal to ground
                        string nDrain = j == 0 ? nandSignalNames[i] : Util.GetSpiceName(uniqueId, i, $"nand{j}");
                        string nSource = j == inputSignalNames.Count - 1 ? "0" : Util.GetSpiceName(uniqueId, i, $"nand{j+1}");
                        returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, $"nnand{j}"), nDrain, inputSignal, nSource, false);
                    }

                    // Add PMOS and NMOS to form NOT gate going from nand signal name to output signal name
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "pnot"), outputSignalNames[i], nandSignalNames[i], "VDD", true);
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "nnot"), outputSignalNames[i], nandSignalNames[i], "0", false);
                    returnVal += "\n";
                }

                return new()
                {
                    SpiceString = returnVal,
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                };
            }

            SignalSpiceObjectOutput OrFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalSpiceObjectInput additionalInput)
            {
                if (!innerExpressions.Any())
                    throw new Exception("Must be at least 1 inner expression for Or Function");

                string uniqueId = additionalInput.UniqueId;
                int dimension = 0; // Set first time through loop
                string returnVal = "";
                // Each entry is an array of 1-per-dimension signals from an inner expression--in a 1-d signal, each entry would just have 1 string
                List<string[]> inputSignalNames = [];

                // Loop through each inner expression--these each correspond to a PMOS and NMOS per dimension
                foreach ((ILogicallyCombinable<ISignal> innerExpression, int i) in innerExpressions.Select((item, i) => (item, i)))
                {
                    // Run function for inner expression to get gate input signal
                    string innerUniqueId = uniqueId + $"_{i}";
                    SignalSpiceObjectOutput innerOutput = innerExpression.GenerateLogicalObject(options, new()
                    {
                        UniqueId = innerUniqueId,
                    });
                    
                    // Add inner entities
                    returnVal += innerOutput.SpiceString;

                    // Set dimension first time through
                    if (i == 0)
                        dimension = innerOutput.Dimension;

                    // Add inner output signal names to list
                    inputSignalNames.Add(innerOutput.OutputSignalNames);
                }

                // Generate nor output signal names and final output signal names (1 per dimension)
                string[] norSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "norout"))];
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "orout"))];

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    // Add a PMOS and NMOS for each input signal
                    foreach ((string inputSignal, int j) in inputSignalNames.Select((s, j) => (s[i], j)))
                    {
                        // PMOSs go in series from VDD to norSignal
                        string pDrain = j == 0 ? norSignalNames[i] : Util.GetSpiceName(uniqueId, i, $"nor{j}");
                        string pSource = j == inputSignalNames.Count - 1 ? "VDD" : Util.GetSpiceName(uniqueId, i, $"nor{j+1}");
                        returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, $"pnor{j}"), pDrain, inputSignal, pSource, true);
                        // NMOSs go in parallel from norSignal to ground
                        returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, $"nnor{j}"), norSignalNames[i], inputSignal, "0", false);
                    }

                    // Add PMOS and NMOS to form NOT gate going from nor signal name to output signal name
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "pnot"), outputSignalNames[i], norSignalNames[i], "VDD", true);
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "nnot"), outputSignalNames[i], norSignalNames[i], "0", false);
                    returnVal += "\n";
                }

                return new()
                {
                    SpiceString = returnVal,
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                };
            }

            SignalSpiceObjectOutput NotFunction(ILogicallyCombinable<ISignal> innerExpression, SignalSpiceObjectInput additionalInput)
            {
                string uniqueId = additionalInput.UniqueId;

                // Run function for inner expression to get gate input signals (1 per dimension)
                string innerUniqueId = uniqueId + "_0";
                SignalSpiceObjectOutput innerOutput = innerExpression.GenerateLogicalObject(options, new()
                {
                    UniqueId = innerUniqueId,
                });
                int dimension = innerOutput.Dimension; // Get dimension from inner

                // Generate output signal names (1 per dimension)
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "notout"))];

                // For each dimension, add PMOS and NMOS to form NOT gate going from inner output signal name to output signal name
                string returnVal = innerOutput.SpiceString;
                for (int i = 0; i < dimension; i++)
                {
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "p"), outputSignalNames[i], innerOutput.OutputSignalNames[i], "VDD", true);
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "n"), outputSignalNames[i], innerOutput.OutputSignalNames[i], "0", false);
                    returnVal += "\n";
                }

                return new()
                {
                    SpiceString = returnVal,
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                };
            }

            SignalSpiceObjectOutput BaseFunction(ISignal innerExpression, SignalSpiceObjectInput additionalInput)
            {
                // TODO check that this works for literal signals
                // Get signals as strings and generate output signal names
                string[] signals = [.. innerExpression.ToSingleNodeSignals.Select(s => s.ToSpice())];
                string uniqueId = additionalInput.UniqueId;
                string[] outputSignals = [.. Enumerable.Range(0, signals.Length).Select(i => Util.GetSpiceName(uniqueId, i, "baseout"))];
                
                // Add 1m resistors between signals and output signals
                string toReturn = string.Join("\n", Enumerable.Range(0, signals.Length).Select(i => 
                    $"R{Util.GetSpiceName(uniqueId, i, "res")} {signals[i]} {outputSignals[i]} 1m\n"));

                return new()
                {
                    SpiceString = toReturn,
                    Dimension = signals.Length,
                    OutputSignalNames = outputSignals,
                };
            }

            options.AndFunction = AndFunction;
            options.OrFunction = OrFunction;
            options.NotFunction = NotFunction;
            options.BaseFunction = BaseFunction;

            signalSpiceObjectOptions = options;
            return options;
        }
    }

    private static CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput>? signalSpiceSharpObjectOptions;

    private static CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput> SignalSpiceSharpObjectOptions
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

                // Generate nand output signal names and final output signal names (1 per dimension)
                string[] nandSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "nandout"))];
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "andout"))];

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    // Add a PMOS and NMOS for each input signal
                    foreach ((string inputSignal, int j) in inputSignalNames.Select((s, j) => (s[i], j)))
                    {
                        // PMOSs go in parallel from VDD to nandSignal
                        entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"pnand{j}")}", nandSignalNames[i], inputSignal, "VDD", "VDD", Util.PmosModelName));
                        // NMOSs go in series from nandSignal to ground
                        string nDrain = j == 0 ? nandSignalNames[i] : Util.GetSpiceName(uniqueId, i, $"nand{j}");
                        string nSource = j == inputSignalNames.Count - 1 ? "0" : Util.GetSpiceName(uniqueId, i, $"nand{j+1}");
                        entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"nnand{j}")}", nDrain, inputSignal, nSource, nSource, Util.NmosModelName));
                    }

                    // Add PMOS and NMOS to form NOT gate going from nand signal name to output signal name
                    entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"pnot")}", outputSignalNames[i], nandSignalNames[i], "VDD", "VDD", Util.PmosModelName));
                    entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"nnot")}", outputSignalNames[i], nandSignalNames[i], "0", "0", Util.NmosModelName));
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

                // Generate nor output signal names and final output signal names (1 per dimension)
                string[] norSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "norout"))];
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "orout"))];

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    // Add a PMOS and NMOS for each input signal
                    foreach ((string inputSignal, int j) in inputSignalNames.Select((s, j) => (s[i], j)))
                    {
                        // PMOSs go in series from VDD to norSignal
                        string pDrain = j == 0 ? norSignalNames[i] : Util.GetSpiceName(uniqueId, i, $"nor{j}");
                        string pSource = j == inputSignalNames.Count - 1 ? "VDD" : Util.GetSpiceName(uniqueId, i, $"nor{j+1}");
                        entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"pnor{j}")}", pDrain, inputSignal, pSource, pSource, Util.PmosModelName));
                        // NMOSs go in parallel from norSignal to ground
                        entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"nnor{j}")}", norSignalNames[i], inputSignal, "0", "0", Util.NmosModelName));
                    }

                    // Add PMOS and NMOS to form NOT gate going from nor signal name to output signal name
                    entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"pnot")}", outputSignalNames[i], norSignalNames[i], "VDD", "VDD", Util.PmosModelName));
                    entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, $"nnot")}", outputSignalNames[i], norSignalNames[i], "0", "0", Util.NmosModelName));
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
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "notout"))];

                // For each dimension, add PMOS and NMOS to form NOT gate going from inner output signal name to output signal name
                List<IEntity> entities = [.. innerOutput.SpiceSharpEntities];
                for (int i = 0; i < dimension; i++)
                {
                    entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, "p")}", outputSignalNames[i], innerOutput.OutputSignalNames[i], "VDD", "VDD", Util.PmosModelName));
                    entities.Add(new Mosfet1($"M{Util.GetSpiceName(uniqueId, i, "n")}", outputSignalNames[i], innerOutput.OutputSignalNames[i], "0", "0", Util.NmosModelName));
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
                string[] signals = [.. innerExpression.ToSingleNodeSignals.Select(s => s.ToSpice())];
                string uniqueId = additionalInput.UniqueId;
                string[] outputSignals = [.. Enumerable.Range(0, signals.Length).Select(i => Util.GetSpiceName(uniqueId, i, "baseout"))];
                
                // Add 1m resistors between signals and output signals
                IEnumerable<IEntity> entities = Enumerable.Range(0, signals.Length).Select(i =>
                    new Resistor($"R{Util.GetSpiceName(uniqueId, i, "res")}", signals[i], outputSignals[i], 1e-3));

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
    public bool IsCompatible(NamedSignal outputSignal) => expression.CanCombine(outputSignal);
}