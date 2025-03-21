using System.Collections.ObjectModel;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.SpiceCircuits;

namespace VHDLSharp.Utility;

/// <summary>
/// Utility class for Spice stuff
/// </summary>
internal static class SpiceUtil
{
    private static Mosfet1Model? nmosModel = null;
    private static Mosfet1Model? pmosModel = null;

    // Mapping of #inputs to subcircuit definition
    private static readonly Dictionary<int, INamedSubcircuitDefinition> andSubcircuits = [];
    private static readonly Dictionary<int, INamedSubcircuitDefinition> orSubcircuits = [];
    private static INamedSubcircuitDefinition? notSubcircuit = null;
    
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

    // TODO commonly-used subcircuits

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
        string[] pins = [.. Enumerable.Range(1, numInputs).Select(i => $"IN{i}").Append("OUT")];

        // Add a PMOS and NMOS for each input signal
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in parallel from VDD to nandSignal
            circuit.Add(new Mosfet1($"pnand{i}", "nand", $"IN{i}", "VDD", "VDD", PmosModel.Name));
            // NMOSs go in series from nandSignal to ground
            string nDrain = i == 0 ? "nand" : $"nand{i}";
            string nSource = i == numInputs - 1 ? "0" : $"nand{i+1}";
            circuit.Add(new Mosfet1($"nnand{i}", nDrain, $"IN{i}", nSource, nSource, NmosModel.Name));
        }

        // Add PMOS and NMOS to form NOT gate going from nand signal name to output signal name
        circuit.Add(new Mosfet1($"pnot", "OUT", "nand", "VDD", "VDD", PmosModel.Name));
        circuit.Add(new Mosfet1($"nnot", "OUT", "nand", "0", "0", NmosModel.Name));

        subcircuit = new NamedSubcircuitDefinition("AND", circuit, pins);
        andSubcircuits[numInputs] = subcircuit;
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
        string[] pins = [.. Enumerable.Range(1, numInputs).Select(i => $"IN{i}").Append("OUT")];

        // Add a PMOS and NMOS for each input signal
        for (int i = 1; i <= numInputs; i++)
        {
            // PMOSs go in series from VDD to norSignal
            string pDrain = i == 0 ? "nor" : $"nor{i}";
            string pSource = i == numInputs - 1 ? "VDD" : $"nor{i+1}";
            circuit.Add(new Mosfet1($"pnor{i}", pDrain, $"IN{i}", pSource, pSource, PmosModel.Name));
            // NMOSs go in parallel from norSignal to ground
            circuit.Add(new Mosfet1($"nnor{i}", "nor", $"IN{i}", "0", "0", NmosModel.Name));
        }

        // Add PMOS and NMOS to form NOT gate going from nor signal name to output signal name
        circuit.Add(new Mosfet1($"pnot", "OUT", "nor", "VDD", "VDD", PmosModel.Name));
        circuit.Add(new Mosfet1($"nnot", "OUT", "nor", "0", "0", NmosModel.Name));

        subcircuit = new NamedSubcircuitDefinition("OR", circuit, pins);
        orSubcircuits[numInputs] = subcircuit;
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