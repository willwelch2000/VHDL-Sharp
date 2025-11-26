using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Utility;

/// <summary>
/// Utility class for Spice stuff
/// </summary>
public static class SpiceUtil
{
    private static Mosfet1Model? nmosModel = null;
    private static Mosfet1Model? pmosModel = null;

    // Mapping of #inputs to subcircuit definition
    private static readonly Dictionary<int, INamedSubcircuitDefinition> andSubcircuits = [];
    private static readonly Dictionary<int, INamedSubcircuitDefinition> orSubcircuits = [];
    private static readonly Dictionary<int, INamedSubcircuitDefinition> nandSubcircuits = [];
    private static readonly Dictionary<int, INamedSubcircuitDefinition> norSubcircuits = [];
    private static readonly Dictionary<int, INamedSubcircuitDefinition> xnorSubcircuits = [];
    private static readonly Dictionary<int, INamedSubcircuitDefinition> muxSubcircuits = [];
    private static INamedSubcircuitDefinition? notSubcircuit = null;
    private static INamedSubcircuitDefinition? dLatchWithOverride = null;
    private static INamedSubcircuitDefinition? dffWithAsyncLoad = null;
    
    internal static double VDD => 5.0;

    /// <summary>
    /// Get singleton NMOS model object
    /// </summary>
    internal static Mosfet1Model NmosModel
    {
        get
        {
            if (nmosModel is not null)
                return nmosModel;

            nmosModel = new("NmosMod");
            nmosModel.Parameters.SetNmos(true);
            nmosModel.Parameters.Width = 100e-6;
            nmosModel.Parameters.Length = 1e-6;
            return nmosModel;
        }
    }

    /// <summary>
    /// Get singleton PMOS model object
    /// </summary>
    internal static Mosfet1Model PmosModel
    {
        get
        {
            if (pmosModel is not null)
                return pmosModel;

            pmosModel = new("PmosMod");
            pmosModel.Parameters.SetPmos(true);
            pmosModel.Parameters.Width = 100e-6;
            pmosModel.Parameters.Length = 1e-6;
            return pmosModel;
        }
    }

    /// <summary>
    /// Get singleton VDD voltage source object
    /// </summary>
    internal static VoltageSource VddSource { get; } = new("VDD", "VDD", "0", VDD);

    /// <summary>
    /// Entities to be included whenever MOSFETs or the VDD node are used
    /// </summary>
    internal static IEnumerable<IEntity> CommonEntities
    {
        get
        {
            yield return NmosModel;
            yield return PmosModel;
            yield return VddSource;
        }
    }

    /// <summary>
    /// Subcircuit for NAND gate
    /// </summary>
    /// <param name="numInputs"></param>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetNandSubcircuit(int numInputs)
    {
        if (nandSubcircuits.TryGetValue(numInputs, out INamedSubcircuitDefinition? subcircuit))
            return subcircuit;

        Circuit circuit = [.. CommonEntities];
        string[] pins = [.. Enumerable.Range(1, numInputs).Select(i => $"IN{i}"), "OUT"];

        // Add a PMOS and NMOS for each input signal
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in parallel from VDD to OUT
            circuit.Add(new Mosfet1($"pnand{i}", "OUT", $"IN{i}", "VDD", "VDD", PmosModel.Name));
            // NMOSs go in series from OUT to ground
            string nDrain = i == 1 ? "OUT" : $"nand{i}";
            string nSource = i == numInputs ? "0" : $"nand{i+1}";
            circuit.Add(new Mosfet1($"nnand{i}", nDrain, $"IN{i}", nSource, nSource, NmosModel.Name));
        }

        subcircuit = new NamedSubcircuitDefinition($"NAND{numInputs}", circuit, pins);
        nandSubcircuits[numInputs] = subcircuit;
        return subcircuit;
    }

    /// <summary>
    /// Subcircuit for AND gate
    /// </summary>
    /// <param name="numInputs"></param>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetAndSubcircuit(int numInputs)
    {
        if (andSubcircuits.TryGetValue(numInputs, out INamedSubcircuitDefinition? subcircuit))
            return subcircuit;

        Circuit circuit = [.. CommonEntities];
        string[] pins = [.. Enumerable.Range(1, numInputs).Select(i => $"IN{i}"), "OUT"];

        // Add a PMOS and NMOS for each input signal
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in parallel from VDD to nandSignal
            circuit.Add(new Mosfet1($"pnand{i}", "nand", $"IN{i}", "VDD", "VDD", PmosModel.Name));
            // NMOSs go in series from nandSignal to ground
            string nDrain = i == 1 ? "nand" : $"nand{i}";
            string nSource = i == numInputs ? "0" : $"nand{i+1}";
            circuit.Add(new Mosfet1($"nnand{i}", nDrain, $"IN{i}", nSource, nSource, NmosModel.Name));
        }

        // Add PMOS and NMOS to form NOT gate going from nand signal name to output signal name
        circuit.Add(new Mosfet1($"pnot", "OUT", "nand", "VDD", "VDD", PmosModel.Name));
        circuit.Add(new Mosfet1($"nnot", "OUT", "nand", "0", "0", NmosModel.Name));

        subcircuit = new NamedSubcircuitDefinition($"AND{numInputs}", circuit, pins);
        andSubcircuits[numInputs] = subcircuit;
        return subcircuit;
    }

    /// <summary>
    /// Subcircuit for NOR gate
    /// </summary>
    /// <param name="numInputs"></param>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetNorSubcircuit(int numInputs)
    {
        if (norSubcircuits.TryGetValue(numInputs, out INamedSubcircuitDefinition? subcircuit))
            return subcircuit;

        Circuit circuit = [.. CommonEntities];
        string[] pins = [.. Enumerable.Range(1, numInputs).Select(i => $"IN{i}"), "OUT"];

        // Add a PMOS and NMOS for each input signal
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in series from VDD to OUT
            string pDrain = i == 1 ? "OUT" : $"nor{i}";
            string pSource = i == numInputs ? "VDD" : $"nor{i+1}";
            circuit.Add(new Mosfet1($"pnor{i}", pDrain, $"IN{i}", pSource, pSource, PmosModel.Name));
            // NMOSs go in parallel from OUT to ground
            circuit.Add(new Mosfet1($"nnor{i}", "OUT", $"IN{i}", "0", "0", NmosModel.Name));
        }

        subcircuit = new NamedSubcircuitDefinition($"NOR{numInputs}", circuit, pins);
        norSubcircuits[numInputs] = subcircuit;
        return subcircuit;
    }

    /// <summary>
    /// Subcircuit for OR gate
    /// </summary>
    /// <param name="numInputs"></param>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetOrSubcircuit(int numInputs)
    {
        if (orSubcircuits.TryGetValue(numInputs, out INamedSubcircuitDefinition? subcircuit))
            return subcircuit;

        Circuit circuit = [.. CommonEntities];
        string[] pins = [.. Enumerable.Range(1, numInputs).Select(i => $"IN{i}"), "OUT"];

        // Add a PMOS and NMOS for each input signal
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in series from VDD to norSignal
            string pDrain = i == 1 ? "nor" : $"nor{i}";
            string pSource = i == numInputs ? "VDD" : $"nor{i+1}";
            circuit.Add(new Mosfet1($"pnor{i}", pDrain, $"IN{i}", pSource, pSource, PmosModel.Name));
            // NMOSs go in parallel from norSignal to ground
            circuit.Add(new Mosfet1($"nnor{i}", "nor", $"IN{i}", "0", "0", NmosModel.Name));
        }

        // Add PMOS and NMOS to form NOT gate going from nor signal name to output signal name
        circuit.Add(new Mosfet1($"pnot", "OUT", "nor", "VDD", "VDD", PmosModel.Name));
        circuit.Add(new Mosfet1($"nnot", "OUT", "nor", "0", "0", NmosModel.Name));

        subcircuit = new NamedSubcircuitDefinition($"OR{numInputs}", circuit, pins);
        orSubcircuits[numInputs] = subcircuit;
        return subcircuit;
    }

    /// <summary>
    /// Subcircuit for XNOR gate
    /// </summary>
    /// <param name="numInputs"></param>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetXnorSubcircuit(int numInputs)
    {
        if (xnorSubcircuits.TryGetValue(numInputs, out INamedSubcircuitDefinition? subcircuit))
            return subcircuit;

        Circuit circuit = [.. CommonEntities];
        string[] pins = [.. Enumerable.Range(1, numInputs).Select(i => $"IN{i}"), "OUT"];

        if (numInputs != 2)
            throw new NotImplementedException("XNOR only supported for 2 inputs");

        // AB + !A!B = A*B + !(A+B) = !!(A*B + !(A+B)) = !(!(A*B)*(A+B))
        circuit.Add(new Subcircuit("nand1", GetNandSubcircuit(2), "IN1", "IN2", "nand1out"));
        circuit.Add(new Subcircuit("or", GetOrSubcircuit(2), "IN1", "IN2", "orout"));
        circuit.Add(new Subcircuit("nand2", GetNandSubcircuit(2), "nand1out", "orout", "OUT"));

        subcircuit = new NamedSubcircuitDefinition($"XNOR{numInputs}", circuit, pins);
        xnorSubcircuits[numInputs] = subcircuit;
        return subcircuit;
    }

    /// <summary>
    /// Subcircuit for NOT gate
    /// </summary>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetNotSubcircuit()
    {
        if (notSubcircuit is not null)
            return notSubcircuit;

        Circuit circuit = [.. CommonEntities];
        string[] pins = ["IN", "OUT"];
        
        circuit.Add(new Mosfet1("p", "OUT", "IN", "VDD", "VDD", PmosModel.Name));
        circuit.Add(new Mosfet1("n", "OUT", "IN", "0", "0", NmosModel.Name));

        notSubcircuit = new NamedSubcircuitDefinition("NOT", circuit, pins);
        return notSubcircuit;
    }


    /// <summary>
    /// Subcircuit for MUX--named MUX{dim}.
    /// Pins are SEL{i} for select bits, then IN{i} for inputs, then OUT for output
    /// </summary>
    /// <param name="dim"></param>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetMuxSubcircuit(int dim)
    {
        if (muxSubcircuits.TryGetValue(dim, out INamedSubcircuitDefinition? subcircuit))
            return subcircuit;

        if (dim < 1)
            throw new Exception("Dim must be >= 1");

        Circuit circuit = [.. CommonEntities];
        string[] pins = [.. Enumerable.Range(1, dim).Select(i => $"SEL{i}"), 
                         .. Enumerable.Range(1, 1<<dim).Select(i => $"IN{i}"), "OUT"];

        // NOTs for the select bits
        for (int i = 1; i <= dim; i++)
            circuit.Add(new Subcircuit($"not{i}", GetNotSubcircuit(), $"SEL{i}", $"SEL{i}_not"));

        // MUX options
        INamedSubcircuitDefinition nand1 = GetNandSubcircuit(1 + dim);
        for (int i = 0; i < 1<<dim; i++)
        {
            List<string> nodes = [$"IN{i+1}"];
            for (int j = 0; j < dim; j++)
                nodes.Add((i & 1<<j) > 0 ? $"SEL{j+1}" : $"SEL{j+1}_not");
            nodes.Add($"int{i+1}");
            circuit.Add(new Subcircuit($"nandIn{i+1}", nand1, [.. nodes]));
        }

        // NAND all together
        circuit.Add(new Subcircuit("final", GetNandSubcircuit(1<<dim), [.. Enumerable.Range(1, 1<<dim).Select(i => $"int{i}"), "OUT"]));
        
        subcircuit = new NamedSubcircuitDefinition($"MUX{dim}", circuit, pins);
        muxSubcircuits[dim] = subcircuit;
        return subcircuit;
    }

    /// <summary>
    /// Subcircuit for D Latch with override. 
    /// Acts like a regular D Latch, but with an override load/input
    /// Pins: D--normal input, DO--override input, 
    /// L--normal load, LO--override load, Q--output
    /// </summary>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetDLatchWithOverrideSubcircuit()
    {
        if (dLatchWithOverride is not null)
            return dLatchWithOverride;

        Circuit circuit = [.. CommonEntities];
        string[] pins = ["D", "DO", "L", "LO", "Q"];

        INamedSubcircuitDefinition nand2 = GetNandSubcircuit(2);
        INamedSubcircuitDefinition inv = GetNotSubcircuit();
        INamedSubcircuitDefinition mux1 = GetMuxSubcircuit(1);

        // NANDs
        circuit.Add(new Subcircuit("nand1", nand2, "D", "L", "net1"));
        circuit.Add(new Subcircuit("inv1", inv, "D", "Db"));
        circuit.Add(new Subcircuit("nand2", nand2, "Db", "L", "net2"));

        // MUXes
        circuit.Add(new Subcircuit("inv2", inv, "DO", "DOb"));
        circuit.Add(new Subcircuit("mux1", mux1, "LO", "net1", "DOb", "Sb"));
        circuit.Add(new Subcircuit("mux2", mux1, "LO", "net2", "DO", "Rb"));

        // SR Latch
        circuit.Add(new Subcircuit("nand3", nand2, "Sb", "Qb", "Q"));
        circuit.Add(new Subcircuit("nand4", nand2, "Rb", "Q", "Qb"));
        
        dLatchWithOverride = new NamedSubcircuitDefinition($"DLatchWithOverride", circuit, pins);
        return dLatchWithOverride;
    }

    internal static Circuit GetSrLatchCircuit()
    {
        Circuit circuit = [.. CommonEntities];
        INamedSubcircuitDefinition nand2 = GetNandSubcircuit(2);
        circuit.Add(new Subcircuit("nand1", nand2, "Sb", "Qb", "Q"));
        circuit.Add(new Subcircuit("nand2", nand2, "Rb", "Q", "Qb"));
        return circuit;
    }

    /// <summary>
    /// Subcircuit for DFF with async load. 
    /// Acts like a regular DFF Latch, but with an additional async load and input
    /// Pins: D--normal input, DA--async input, 
    /// CLK--synchronous clock, LA--async load, Q--output
    /// </summary>
    /// <returns></returns>
    internal static INamedSubcircuitDefinition GetDffWithAsyncLoadSubcircuit()
    {
        if (dffWithAsyncLoad is not null)
            return dffWithAsyncLoad;

        Circuit circuit = [.. CommonEntities];
        string[] pins = ["D", "DA", "CLK", "LA", "Q"];

        INamedSubcircuitDefinition inv = GetNotSubcircuit();
        INamedSubcircuitDefinition latch = GetDLatchWithOverrideSubcircuit();

        // CLK inverters
        circuit.Add(new Subcircuit("inv1", inv, "CLK", "CLKb"));
        circuit.Add(new Subcircuit("inv2", inv, "CLKb", "CLK2"));

        // Latches
        circuit.Add(new Subcircuit("latch1", latch, "D", "DA", "CLKb", "LA", "Qmid"));
        circuit.Add(new Subcircuit("latch2", latch, "Qmid", "DA", "CLK2", "LA", "Q"));

        dffWithAsyncLoad = new NamedSubcircuitDefinition($"DFFWithAsyncLoad", circuit, pins);
        return dffWithAsyncLoad;
    }

    /// <summary>
    /// Method to generate Spice node name
    /// </summary>
    /// <param name="uniqueId">Unique id for portion of the circuit</param>
    /// <param name="dimensionIndex">Number given to differentiate duplicates for multi-dimensional signals</param>
    /// <param name="ending">Name given to node to differentiate within portion</param>
    /// <returns></returns>
    internal static string GetSpiceName(string uniqueId, int dimensionIndex, string ending) => $"n{uniqueId}x{dimensionIndex}_{ending}";

    /// <summary>
    /// Get Spice for a specific Spice# entity. Does not add a new line.
    /// Prepends Spice letters to the beginning
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    internal static string GetSpice(this IEntity entity) => entity switch
    {
        // Note: this does the bare minimum, does not exhaustively convert entities to string
        Mosfet1 mosfet1 => $"M{mosfet1.Name} {string.Join(' ', mosfet1.Nodes)} {mosfet1.Model}",
        Resistor resistor => $"R{resistor.Name} {string.Join(' ', resistor.Nodes)} {resistor.Parameters.Resistance.Value}",
        Mosfet1Model mosfet1Model => $".MODEL {mosfet1Model.Name} {mosfet1Model.Parameters.TypeName} W={mosfet1Model.Parameters.Width.Value} L={mosfet1Model.Parameters.Length.Value}",
        VoltageSource voltageSource => $"V{voltageSource.Name} {string.Join(' ', voltageSource.Nodes)} " + voltageSource switch
        {
            {Parameters.Waveform : Pwl pwl} => "PWL(" + string.Join(" ", pwl.Points.Select(p => $"{p.Time:G7} {p.Value:G7}")) + ")",
            {Parameters.Waveform : Pulse pulse} => $"PULSE({pulse.InitialValue} {pulse.PulsedValue} {pulse.Delay} {pulse.RiseTime} {pulse.FallTime} {pulse.PulseWidth} {pulse.Period})",
            _ => $"{voltageSource.Parameters.DcValue.Value}",
        },
        Subcircuit subcircuit => $"X{subcircuit.Name} {string.Join(' ', subcircuit.Nodes)} " + subcircuit.Parameters.Definition switch
        {
            INamedSubcircuitDefinition namedSubDef => namedSubDef.Name,
            _ => throw new Exception("Cannot get Spice as string for subcircuit without named definition")
        },
        _ => throw new Exception("Unknown entity type")
    };

    /// <summary>
    /// True if the entity is a model
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    internal static bool IsModel(this IEntity entity) => entity is Mosfet1Model or Mosfet2Model or Mosfet3Model;
}