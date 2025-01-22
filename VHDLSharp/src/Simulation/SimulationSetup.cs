
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using SpiceSharp.Simulations;
using SpiceSharp.Simulations.Base;
using VHDLSharp.Modules;
using VHDLSharp.Utility;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a simulation setup
/// </summary>
public class SimulationSetup
{
    /// <summary>
    /// Create simulation setup given module to simulate
    /// </summary>
    /// <param name="module">Module that is simulated</param>
    public SimulationSetup(Module module)
    {
        StimulusMapping = new(module);
        Module = module;
        SignalsToMonitor = [];
        SignalsToMonitor.CollectionChanged += CheckValidNewItem;
    }

    /// <summary>
    /// Mapping of module's ports to stimuli
    /// </summary>
    public StimulusMapping StimulusMapping { get; }

    /// <summary>
    /// Module that has stimuli applied
    /// </summary>
    public Module Module { get; }

    /// <summary>
    /// List of signals to receive output for
    /// </summary>
    public ObservableCollection<SignalReference> SignalsToMonitor { get; }

    /// <summary>
    /// How long the simulation should be
    /// </summary>
    public double Length { get; set; } = 1e-3;

    /// <summary>
    /// Time between steps
    /// </summary>
    public double StepSize { get; set; } = 1e-6;

    /// <summary>
    /// Assign a stimulus set to a port
    /// </summary>
    /// <param name="port"></param>
    /// <param name="stimulus"></param>
    public void AssignStimulus(Port port, IStimulusSet stimulus) => StimulusMapping[port] = stimulus;

    /// <summary>
    /// True if ready to convert to Spice or simulate
    /// </summary>
    public bool Complete() => StimulusMapping.Complete();

    /// <summary>
    /// Get Spice representation of the setup
    /// </summary>
    /// <returns></returns>
    public string ToSpice()
    {
        if (!Complete())
            throw new Exception("Simulation setup must be complete to convert to Spice");

        string toReturn = Module.ToSpice();

        // Connect stimuli to ports
        int i = 0;
        foreach ((Port port, IStimulusSet stimulus) in StimulusMapping)
            toReturn += $"{stimulus.ToSpice(port.Signal, i++.ToString())}\n";

        return toReturn;
    }

    /// <summary>
    /// Get Spice# Circuit representation of setup
    /// </summary>
    /// <returns></returns>
    public Circuit ToSpiceSharpCircuit()
    {
        if (!Complete())
            throw new Exception("Simulation setup must be complete to convert to circuit");

        Circuit circuit = Module.ToSpiceSharpCircuit();

        // Connect stimuli to ports
        int i = 0;
        foreach ((Port port, IStimulusSet stimulus) in StimulusMapping)
            foreach (IEntity entity in stimulus.ToSpiceSharpEntities(port.Signal, i++.ToString()))
                circuit.Add(entity);

        return circuit;
    }

    /// <inheritdoc/>
    public IEnumerable<SimulationResult> Simulate()
    {
        Circuit circuit = ToSpiceSharpCircuit();
        
        var tran = new Transient("Tran 1", StepSize, Length);

        // List of Spice# Reference objects and SimulationResult objects
        List<(RealVoltageExport export, SimulationResult result)> spiceSharpRefs = [];
        foreach (SignalReference signalReference in SignalsToMonitor)
        {
            Reference spiceSharpRef = signalReference.GetSpiceSharpReference();
            SimulationResult result = new(signalReference);
            spiceSharpRefs.Add((new RealVoltageExport(tran, spiceSharpRef), result));
        }

        double time = StepSize;
        IEntity[] entities = circuit.ToArray();
        Circuit circuit2 = [];
        circuit2.Add(new VoltageSource("V_VDD", "VDD", "0", Util.VDD));
        Mosfet1Model nmosModel = new(Util.NmosModelName);
        nmosModel.Parameters.SetNmos(true);
        Mosfet1Model pmosModel = new(Util.PmosModelName);
        pmosModel.Parameters.SetPmos(true);
        circuit2.Add(nmosModel);
        circuit2.Add(pmosModel);
        circuit2.Add(new Resistor("Rn0_0x0_res", "s1", "n0_0x0_baseout", 1));
        circuit2.Add(new Resistor("Rn0_1x0_res", "s2", "n0_1x0_baseout", 1));
        circuit2.Add(new Mosfet1("Mn0x0_pnand0", "n0x0_nandout", "n0_0x0_baseout", "VDD", "VDD", "PmosMod"));
        circuit2.Add(new Mosfet1("Mn0x0_nnand0", "n0x0_nandout", "n0_0x0_baseout", "0", "0", "NmosMod"));

        circuit2.Add(new VoltageSource("V_1", "s1", "0", Util.VDD));
        circuit2.Add(new VoltageSource("V_2", "n0x0_nandout", "0", 3));
        circuit2.Add(new VoltageSource("V_3", "n0_1x0_baseout", "0", 3));
        circuit2.Add(new VoltageSource("V_4", "s2", "0", Util.VDD));
        foreach (int _ in tran.Run(circuit2, Transient.ExportTransient))
        {
            foreach ((RealVoltageExport export, SimulationResult result) in spiceSharpRefs)
            {
                result.AddTimeStepValue(time, export.Value);
                time += StepSize;
            }
        }

        return spiceSharpRefs.Select(r => r.result);
    }

    private void CheckValidNewItem(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Check that reference has correct top-level module
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
                if (newItem is SignalReference signalReference && signalReference.TopLevelModule != Module)
                    throw new Exception($"Added signal reference must use module {Module} as top-level module");
    }
}