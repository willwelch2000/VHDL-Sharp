
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SpiceSharp;
using SpiceSharp.Entities;
using SpiceSharp.Simulations;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Validation;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a simulation setup
/// </summary>
public class SimulationSetup : IValidityManagedEntity
{
    private readonly ValidityManager manager;

    private readonly ObservableCollection<object> trackedEntities;

    private EventHandler? updated;

    private double length = 1e-3;

    private double stepSize = 1e-6;

    /// <summary>
    /// Create simulation setup given module to simulate
    /// </summary>
    /// <param name="module">Module that is simulated</param>
    public SimulationSetup(IModule module)
    {
        StimulusMapping = new(module);
        Module = module;
        trackedEntities = [StimulusMapping, module];
        manager = new(this, trackedEntities);
        SignalsToMonitor = [];
        SignalsToMonitor.CollectionChanged += SignalsListUpdated;
    }

    ValidityManager IValidityManagedEntity.ValidityManager => manager;

    /// <summary>
    /// Event called when a property of this setup is changed that could affect other objects
    /// </summary>
    public event EventHandler? Updated
    {
        add
        {
            updated -= value; // remove if already present
            updated += value;
        }
        remove => updated -= value;
    }

    /// <summary>
    /// Mapping of module's ports to stimuli
    /// </summary>
    public StimulusMapping StimulusMapping { get; }

    /// <summary>
    /// Module that has stimuli applied
    /// </summary>
    public IModule Module { get; }

    /// <summary>
    /// List of signals to receive output for
    /// </summary>
    public ObservableCollection<SignalReference> SignalsToMonitor { get; }

    /// <summary>
    /// How long the simulation should be
    /// </summary>
    public double Length
    {
        get => length;
        set
        {
            updated?.Invoke(this, EventArgs.Empty);
            length = value;
        }
    }

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
    /// Assign a stimulus set to a port
    /// </summary>
    /// <param name="port"></param>
    /// <param name="stimulus"></param>
    public void AssignStimulus(IPort port, IStimulusSet stimulus) => StimulusMapping[port] = stimulus;

    /// <summary>
    /// True if ready to convert to Spice or simulate
    /// </summary>
    public bool IsComplete() => StimulusMapping.IsComplete();

    /// <summary>
    /// Get Spice representation of the setup
    /// </summary>
    /// <returns></returns>
    public string GetSpice()
    {
        if (!IsComplete())
            throw new IncompleteException("Simulation setup must be complete to convert to Spice");

        string toReturn = Module.GetSpice();

        // Connect stimuli to ports
        int i = 0;
        foreach ((IPort port, IStimulusSet stimulus) in StimulusMapping)
            toReturn += $"{stimulus.GetSpice(port.Signal, i++.ToString())}\n";

        return toReturn;
    }

    /// <summary>
    /// Get Spice# Circuit representation of setup
    /// </summary>
    /// <returns></returns>
    public Circuit GetSpiceSharpCircuit()
    {
        if (!IsComplete())
            throw new IncompleteException("Simulation setup must be complete to convert to circuit");

        Circuit circuit = Module.GetSpiceSharpCircuit();

        // Connect stimuli to ports
        int i = 0;
        foreach ((IPort port, IStimulusSet stimulus) in StimulusMapping)
            foreach (IEntity entity in stimulus.GetSpiceSharpEntities(port.Signal, i++.ToString()))
                circuit.Add(entity);

        return circuit;
    }

    /// <inheritdoc/>
    public IEnumerable<SimulationResult> Simulate()
    {
        Circuit circuit = GetSpiceSharpCircuit();
        
        var tran = new Transient("Tran 1", StepSize, Length);

        // Create all SimulationResult objects
        List<SimulationResult> results = [];
        foreach (SignalReference signalReference in SignalsToMonitor)
            results.Add(new SimulationResult(signalReference, tran));

        // At each timestep, have the SimulationResult objects add the current x-y value
        foreach (int _ in tran.Run(circuit, Transient.ExportTransient))
            foreach (SimulationResult result in results)
                result.AddCurrentTimeStepValue();

        return results;
    }

    private void SignalsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Check that reference has correct top-level module
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
                if (newItem is SignalReference signalReference && signalReference.TopLevelModule != Module)
                    throw new Exception($"Added signal reference must use module {Module} as top-level module");

        updated?.Invoke(this, e);
    }
}