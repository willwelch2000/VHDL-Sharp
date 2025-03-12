using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.Dimensions;
using VHDLSharp.LogicTree;
using VHDLSharp.Utility;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for any type of signal that can be used in an expression.
/// It is assumed that parent-child relationships, as well as the parent module, are not changed after construction.
/// An implementation that breaks this rule could cause validation issues. 
/// </summary>
public interface ISignal : ILogicallyCombinable<ISignal>
{
    /// <summary>
    /// Object explaining how many nodes are part of this signal (1 for normal signal)
    /// </summary> 
    public DefiniteDimension Dimension { get; }

    /// <summary>
    /// Indexer for multi-dimensional signals
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

        // Find named signal, if it exists
        INamedSignal? namedSignal = baseSignals.FirstOrDefault(s => s is INamedSignal) as INamedSignal;
        if (namedSignal is not null)
        {
            // If any signal has another parent
            if (baseSignals.Any(s => s is INamedSignal namedS && namedS.ParentModule != namedSignal.ParentModule))
                return false;
        }

        // If any signal has incompatible dimension with first
        ISignal first = baseSignals.First();
        if (baseSignals.Any(s => !s.Dimension.Compatible(first.Dimension)))
            return false;

        return true;
    }

    internal static CustomLogicObjectOptions<ISignal, SignalSpiceObjectInput, SignalSpiceObjectOutput> SignalSpiceObjectOptions 
    {
        get
        {
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
                string[] signals = [.. innerExpression.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
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

            return options;
        }
    }

    internal static CustomLogicObjectOptions<ISignal, SignalSpiceSharpObjectInput, SignalSpiceSharpObjectOutput> SignalSpiceSharpObjectOptions
    {
        get
        {
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
                string[] signals = [.. innerExpression.ToSingleNodeSignals.Select(s => s.GetSpiceName())];
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

            return options;
        }
    }

    internal static CustomLogicObjectOptions<ISignal, SignalVhdlObjectInput, SignalVhdlObjectOutput> SignalVhdlObjectOptions
    {
        get
        {
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

            return options;
        }
    }

}