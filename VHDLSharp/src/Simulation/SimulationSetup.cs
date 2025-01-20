
using SpiceSharp;
using SpiceSharp.Entities;
using SpiceSharp.Simulations;
using SpiceSharp.Simulations.Base;
using VHDLSharp.Modules;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a simulation setup
/// </summary>
/// <param name="module">Module that is simulated</param>
public class SimulationSetup(Module module)
{
    /// <summary>
    /// Mapping of module's ports to stimuli
    /// </summary>
    public StimulusMapping StimulusMapping { get; } = new(module);

    /// <summary>
    /// Module that has stimuli applied
    /// </summary>
    public Module Module { get; } = module;

    /// <summary>
    /// List of signals to receive output for
    /// </summary>
    public List<SignalReference> SignalsToMonitor { get; } = [];

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
    /// Get Spice representation of the setup
    /// </summary>
    /// <returns></returns>
    public string ToSpice()
    {
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
    public Circuit ToCircuit()
    {
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
        Circuit circuit = ToCircuit();
        
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
        foreach (int _ in tran.Run(circuit, Transient.ExportTransient))
        {
            foreach ((RealVoltageExport export, SimulationResult result) in spiceSharpRefs)
            {
                result.AddTimeStepValue(time, export.Value);
                time += StepSize;
            }
        }

        return spiceSharpRefs.Select(r => r.result);
    }
}