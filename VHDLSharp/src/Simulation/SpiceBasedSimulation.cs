using SpiceSharp;
using SpiceSharp.Simulations;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Validation;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a Spice-based simulation of a <see cref="IModule"/>, using Spice#
/// </summary>
/// <param name="module">Module that is simulated</param>
public class SpiceBasedSimulation(IModule module) : Simulation(module)
{
    private double stepSize = 1e-6;

    /// <summary>
    /// Time between steps
    /// </summary>
    public double StepSize
    {
        get => stepSize;
        set
        {
            updated?.Invoke(this, EventArgs.Empty);
            stepSize = value;
        }
    }

    /// <summary>
    /// Get Spice# Circuit representation of setup
    /// </summary>
    /// <returns></returns>
    public SpiceCircuit GetSpice()
    {
        if (!ValidityManager.IsValid())
            throw new InvalidException("Simulation setup must be valid to convert to Spice# circuit");
        if (!IsComplete())
            throw new IncompleteException("Simulation setup must be complete to convert to circuit");

        SpiceCircuit circuit = Module.GetSpice();

        // Connect stimuli to ports
        List<SpiceCircuit> additionalCircuits = [];
        int i = 0;
        foreach ((IPort port, IStimulusSet stimulus) in StimulusMapping)
            additionalCircuits.Add(stimulus.GetSpice(port.Signal, i++.ToString()));

        // Combine module circuit with additional entities
        return SpiceCircuit.Combine([circuit, .. additionalCircuits]);
    }

    /// <inheritdoc/>
    public override IEnumerable<ISimulationResult> Simulate()
    {
        Circuit circuit = GetSpice().AsCircuit();
        
        var tran = new Transient("Tran 1", StepSize, Length);

        // Create all SpiceSimulationResult objects
        List<SpiceSimulationResult> results = [];
        foreach (SignalReference signalReference in SignalsToMonitor)
            results.Add(new SpiceSimulationResult(signalReference, tran));

        // At each timestep, have the SpiceSimulationResult objects add the current x-y value
        foreach (int _ in tran.Run(circuit, Transient.ExportTransient))
            foreach (SpiceSimulationResult result in results)
                result.AddCurrentTimeStepValue();

        return results;
    }
}