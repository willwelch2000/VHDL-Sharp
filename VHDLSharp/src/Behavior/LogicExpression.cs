using System.Text.RegularExpressions;
using VHDLSharp.Dimensions;
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

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other) => expression.CanCombine(other);

    /// <inheritdoc/>
    public string ToLogicString() => expression.ToLogicString();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => expression.ToLogicString(options);

    /// <inheritdoc/>
    public (string Value, TOut Additional) ToLogicString<TIn, TOut>(CustomLogicStringOptions<ISignal, TIn, TOut> options, TIn additionalInput) where TOut : new()
    {
        return expression.ToLogicString(options, additionalInput);
    }

    /// <summary>
    /// Gets Spice representation of logical expression of signals
    /// </summary>
    /// <param name="outputSignal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    public string ToSpice(NamedSignal outputSignal, string uniqueId)
    {
        (string value, SignalCustomLogicStringOutput additional) = expression.ToLogicString(SignalCustomLogicStringOptions, new()
        {
            UniqueId = uniqueId,
        });

        SingleNodeNamedSignal[] singleNodeSignals = [.. outputSignal.ToSingleNodeNamedSignals];
        if (additional.Dimension != singleNodeSignals.Length)
            throw new Exception("Expression dimension didn't match output signal dimension");

        // Convert final output signals from additional into correct output signals
        for (int i = 0; i < additional.Dimension; i++)
        {
            string newSignalName = singleNodeSignals[i].ToSpice();
            string oldSignalName = additional.OutputSignalNames[i];

            value = Regex.Replace(value, $@"\b{oldSignalName}\b", newSignalName);
        }

        return value;
    }

    /// <summary>
    /// Generate an And with this expression and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public LogicExpression And(ILogicallyCombinable<ISignal> other) => new(new And<ISignal>(expression, other));

    /// <summary>
    /// Generate an Or with this expression and another <see cref="ILogicallyCombinable{T}"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public LogicExpression Or(ILogicallyCombinable<ISignal> other) => new(new And<ISignal>(expression, other));

    /// <summary>
    /// Generate a Not with this expression
    /// </summary>
    /// <returns></returns>
    public LogicExpression Not() => new(new Not<ISignal>(expression));

    private static CustomLogicStringOptions<ISignal, SignalCustomLogicStringInput, SignalCustomLogicStringOutput>? signalCustomLogicStringOptions;

    private static CustomLogicStringOptions<ISignal, SignalCustomLogicStringInput, SignalCustomLogicStringOutput> SignalCustomLogicStringOptions 
    {
        get
        {
            if (signalCustomLogicStringOptions is not null)
                return signalCustomLogicStringOptions;

            CustomLogicStringOptions<ISignal, SignalCustomLogicStringInput, SignalCustomLogicStringOutput> options = new();

            // In each of the following functions, the functionality is done once per dimension in parallel

            (string, SignalCustomLogicStringOutput) AndFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalCustomLogicStringInput additionalInput)
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
                    (string val, SignalCustomLogicStringOutput innerAdditionalOutput) = innerExpression.ToLogicString(options, new()
                    {
                        UniqueId = innerUniqueId,
                    });
                    returnVal += val;

                    // Set dimension first time through
                    if (i == 0)
                        dimension = innerAdditionalOutput.Dimension;

                    // Add inner output signal names to list
                    inputSignalNames.Add(innerAdditionalOutput.OutputSignalNames);
                }

                // Generate nand output signal names and final output signal names (1 per dimension)
                string[] nandSignalNames = Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "nandout")).ToArray();
                string[] outputSignalNames = Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "out")).ToArray();

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
                        string nSource = j == dimension - 1 ? "0" : Util.GetSpiceName(uniqueId, i, $"nand{j+1}");
                        returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, $"nnand{j}"), nDrain, inputSignal, nSource, false);
                    }

                    // Add PMOS and NMOS to form NOT gate going from nand signal name to output signal name
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "pnot"), outputSignalNames[i], nandSignalNames[i], "VDD", true);
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "nnot"), outputSignalNames[i], nandSignalNames[i], "0", false);
                    returnVal += "\n";
                }

                return (returnVal, new()
                {
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                });
            }

            (string, SignalCustomLogicStringOutput) OrFunction(IEnumerable<ILogicallyCombinable<ISignal>> innerExpressions, SignalCustomLogicStringInput additionalInput)
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
                    (string val, SignalCustomLogicStringOutput innerAdditionalOutput) = innerExpression.ToLogicString(options, new()
                    {
                        UniqueId = innerUniqueId,
                    });
                    returnVal += val;

                    // Set dimension first time through
                    if (i == 0)
                        dimension = innerAdditionalOutput.Dimension;

                    // Add inner output signal names to list
                    inputSignalNames.Add(innerAdditionalOutput.OutputSignalNames);
                }

                // Generate nor output signal names and final output signal names (1 per dimension)
                string[] norSignalNames = Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "nandout")).ToArray();
                string[] outputSignalNames = Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "out")).ToArray();

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    // Add a PMOS and NMOS for each input signal
                    foreach ((string inputSignal, int j) in inputSignalNames.Select((s, j) => (s[i], j)))
                    {
                        // PMOSs go in series from VDD to norSignal
                        string pDrain = j == 0 ? norSignalNames[i] : Util.GetSpiceName(uniqueId, i, $"nor{j}");
                        string pSource = j == dimension - 1 ? "VDD" : Util.GetSpiceName(uniqueId, i, $"nor{j+1}");
                        returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, $"pnor{j}"), pDrain, inputSignal, pSource, true);
                        // NMOSs go in parallel from norSignal to ground
                        returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, $"nnor{j}"), norSignalNames[i], inputSignal, "0", false);
                    }

                    // Add PMOS and NMOS to form NOT gate going from nor signal name to output signal name
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "pnot"), outputSignalNames[i], norSignalNames[i], "VDD", true);
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "nnot"), outputSignalNames[i], norSignalNames[i], "0", false);
                    returnVal += "\n";
                }

                return (returnVal, new()
                {
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                });
            }

            (string, SignalCustomLogicStringOutput) NotFunction(ILogicallyCombinable<ISignal> innerExpression, SignalCustomLogicStringInput additionalInput)
            {
                string uniqueId = additionalInput.UniqueId;

                // Run function for inner expression to get gate input signals (1 per dimension)
                string innerUniqueId = uniqueId + "_1";
                (string returnVal, SignalCustomLogicStringOutput innerAdditionalOutput) = innerExpression.ToLogicString(options, new()
                {
                    UniqueId = innerUniqueId,
                });
                int dimension = innerAdditionalOutput.Dimension; // Get dimension from inner

                // Generate output signal names (1 per dimension)
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => Util.GetSpiceName(uniqueId, i, "out"))];

                // For each dimension, add PMOS and NMOS to form NOT gate going from inner output signal name to output signal name
                for (int i = 0; i < dimension; i++)
                {
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "p"), outputSignalNames[i], innerAdditionalOutput.OutputSignalNames[i], "VDD", true);
                    returnVal += Util.GetMosfetSpiceLine(Util.GetSpiceName(uniqueId, i, "n"), outputSignalNames[i], innerAdditionalOutput.OutputSignalNames[i], "0", false);
                    returnVal += "\n";
                }

                return (returnVal, new()
                {
                    Dimension = dimension,
                    OutputSignalNames = outputSignalNames,
                });
            }

            (string, SignalCustomLogicStringOutput) BaseFunction(ISignal innerExpression, SignalCustomLogicStringInput additionalInput)
            {
                string[] signals = [.. innerExpression.ToSingleNodeSignals.Select(s => s.ToSpice())];
                return ("", new()
                {
                    Dimension = signals.Length,
                    OutputSignalNames = signals,
                });
            }

            options.AndFunction = AndFunction;
            options.OrFunction = OrFunction;
            options.NotFunction = NotFunction;
            options.BaseFunction = BaseFunction;

            signalCustomLogicStringOptions = options;
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
    /// Get dimension of this expression
    /// Works by getting dimension from first signal in expression
    /// Valid because signals have definite dimensions
    /// </summary>
    /// <returns></returns>
    public DefiniteDimension? GetDimension() => expression.BaseObjects.FirstOrDefault()?.Dimension;
}