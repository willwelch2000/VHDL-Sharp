using System.Text.RegularExpressions;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Signals;

namespace VHDLSharp.Utility;

internal static class Utility
{
    internal static string AddIndentation(this string s, int indents)
    {
        return string.Concat(Enumerable.Repeat("\t", indents)) + s.ReplaceLineEndings($"\n{string.Concat(Enumerable.Repeat("\t", indents))}");
    }

    internal static string ToBinaryString(this int i, int digits)
    {
        if (i < 0 || i > (1<<digits)-1)
            throw new ArgumentException("i must be between 0 and 2^digits");
        
        string conversion = Convert.ToString(i, 2);
        if (conversion.Length < digits)
            conversion = string.Concat(Enumerable.Repeat('0', digits - conversion.Length)) + conversion;

        return conversion;
    }

    /// <summary>
    /// Works by getting dimension from first signal in expression
    /// Valid because signals have definite dimensions
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    internal static DefiniteDimension? GetDimension(this ILogicallyCombinable<ISignal> expression) => expression.BaseObjects.FirstOrDefault()?.Dimension;

    internal static bool CanCombine<T>(this IEnumerable<ILogicallyCombinable<T>?> expressions) where T : ILogicallyCombinable<T>
    {
        ILogicallyCombinable<T>[] array = expressions.Where(e => e is not null).ToArray()!;
        if (array.Length < 2)
            return true;

        for (int i = 0; i < array.Length-1; i++)
        {
            ILogicallyCombinable<T> first = array[i];
            for (int j = i+1; j < array.Length; j++)
            {
                ILogicallyCombinable<T> second = array[j];
                if (!first.CanCombine(second) || !second.CanCombine(first))
                    return false;
            }
        }

        return true;
    }

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
                string[] nandSignalNames = Enumerable.Range(0, dimension).Select(i => GetSpiceName(uniqueId, i, "nandout")).ToArray();
                string[] outputSignalNames = Enumerable.Range(0, dimension).Select(i => GetSpiceName(uniqueId, i, "out")).ToArray();

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    // Add a PMOS and NMOS for each input signal
                    foreach ((string inputSignal, int j) in inputSignalNames.Select((s, j) => (s[i], j)))
                    {
                        // PMOSs go in parallel from VDD to nandSignal
                        returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, $"pnand{j}"), nandSignalNames[i], inputSignal, "VDD", true);
                        // NMOSs go in series from nandSignal to ground
                        string nDrain = j == 0 ? nandSignalNames[i] : GetSpiceName(uniqueId, i, $"nand{j}");
                        string nSource = j == dimension - 1 ? "0" : GetSpiceName(uniqueId, i, $"nand{j+1}");
                        returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, $"nnand{j}"), nDrain, inputSignal, nSource, false);
                    }

                    // Add PMOS and NMOS to form NOT gate going from nand signal name to output signal name
                    returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, "pnot"), outputSignalNames[i], nandSignalNames[i], "VDD", true);
                    returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, "nnot"), outputSignalNames[i], nandSignalNames[i], "0", false);
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
                string[] norSignalNames = Enumerable.Range(0, dimension).Select(i => GetSpiceName(uniqueId, i, "nandout")).ToArray();
                string[] outputSignalNames = Enumerable.Range(0, dimension).Select(i => GetSpiceName(uniqueId, i, "out")).ToArray();

                // For each dimension...
                for (int i = 0; i < dimension; i++)
                {
                    // Add a PMOS and NMOS for each input signal
                    foreach ((string inputSignal, int j) in inputSignalNames.Select((s, j) => (s[i], j)))
                    {
                        // PMOSs go in series from VDD to norSignal
                        string pDrain = j == 0 ? norSignalNames[i] : GetSpiceName(uniqueId, i, $"nor{j}");
                        string pSource = j == dimension - 1 ? "VDD" : GetSpiceName(uniqueId, i, $"nor{j+1}");
                        returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, $"pnor{j}"), pDrain, inputSignal, pSource, true);
                        // NMOSs go in parallel from norSignal to ground
                        returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, $"nnor{j}"), norSignalNames[i], inputSignal, "0", false);
                    }

                    // Add PMOS and NMOS to form NOT gate going from nor signal name to output signal name
                    returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, "pnot"), outputSignalNames[i], norSignalNames[i], "VDD", true);
                    returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, "nnot"), outputSignalNames[i], norSignalNames[i], "0", false);
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
                string[] outputSignalNames = [.. Enumerable.Range(0, dimension).Select(i => GetSpiceName(uniqueId, i, "out"))];

                // For each dimension, add PMOS and NMOS to form NOT gate going from inner output signal name to output signal name
                for (int i = 0; i < dimension; i++)
                {
                    returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, "p"), outputSignalNames[i], innerAdditionalOutput.OutputSignalNames[i], "VDD", true);
                    returnVal += GetMosfetSpiceLine(GetSpiceName(uniqueId, i, "n"), outputSignalNames[i], innerAdditionalOutput.OutputSignalNames[i], "0", false);
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
    /// Gets Spice representation of logical expression of signals
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="outputSignal"></param>
    /// <param name="uniqueId"></param>
    /// <returns></returns>
    internal static string ToSpice(this ILogicallyCombinable<ISignal> expression, NamedSignal outputSignal, string uniqueId)
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
    /// Method to generate Spice node name
    /// </summary>
    /// <param name="uniqueId">Unique id for portion of the circuit</param>
    /// <param name="indexInCircuitPortion">Number given to differentiate duplicates for multi-dimensional signals</param>
    /// <param name="ending">Name given to node to differentiate within portion</param>
    /// <returns></returns>
    private static string GetSpiceName(string uniqueId, int indexInCircuitPortion, string ending) => $"n{uniqueId}x{indexInCircuitPortion}_{ending}";

    private static string GetMosfetSpiceLine(string name, string drain, string gate, string source, bool pmos) => $"M{name} {drain} {gate} {source} {source} {(pmos ? "PmosMod" : "NmosMod")} W=100u L=1u\n";
}