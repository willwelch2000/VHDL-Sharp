using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Exceptions;
using VHDLSharp.Modules;
using VHDLSharp.Validation;

namespace VHDLSharp.Simulations;

/// <summary>
/// Class representing a simulation setup
/// </summary>
public abstract class Simulation : ISimulation, IValidityManagedEntity, ICompletable
{
    private readonly ValidityManager manager;

    private readonly ObservableCollection<object> childEntities;

    /// <summary>
    /// Event to be called when the object is updated
    /// </summary>
    protected EventHandler? updated;

    private double length = 1e-3;

    /// <summary>
    /// Create simulation setup given module to simulate
    /// </summary>
    /// <param name="module">Module that is simulated</param>
    public Simulation(IModule module)
    {
        StimulusMapping = new(module);
        Module = module;
        childEntities = [StimulusMapping, module];
        manager = new ValidityManager<object>(this, childEntities);
        SignalsToMonitor = [];
        SignalsToMonitor.CollectionChanged += SignalsListUpdated;
    }

    ValidityManager IValidityManagedEntity.ValidityManager => manager;

    /// <summary>
    /// Validity manager for this simulation
    /// </summary>
    protected ValidityManager ValidityManager => manager;

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

    /// <inheritdoc/>
    public StimulusMapping StimulusMapping { get; }

    /// <inheritdoc/>
    public IModule Module { get; }

    /// <summary>
    /// List of signals to receive output for
    /// </summary>
    public ObservableCollection<SignalReference> SignalsToMonitor { get; }

    ICollection<SignalReference> ISimulation.SignalsToMonitor => SignalsToMonitor;

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
    /// Assign a stimulus set to a port
    /// </summary>
    /// <param name="port"></param>
    /// <param name="stimulus"></param>
    public void AssignStimulus(IPort port, IStimulusSet stimulus) => StimulusMapping[port] = stimulus;

    /// <summary>
    /// True if ready to convert to Spice or simulate
    /// </summary>
    /// <param name="reason">Explanation for why it's not complete</param>
    /// <returns></returns>
    public bool IsComplete([MaybeNullWhen(true)] out string reason) => StimulusMapping.IsComplete(out reason);

    /// <inheritdoc/>
    public IEnumerable<ISimulationResult> Simulate()
    {
        if (!manager.IsValid(out Exception? issue))
            throw new InvalidException("Simulation setup must be valid to simulate", issue);
        if (!IsComplete(out string? reason))
            throw new IncompleteException($"Simulation setup must be complete to simulate: {reason}");
        return SimulateWithoutCheck();
    }

    /// <summary>
    /// Simulate module assuming validity and completion checks are already done
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<ISimulationResult> SimulateWithoutCheck();

    private void SignalsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Check that reference has correct top-level module
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            foreach (object newItem in e.NewItems)
            {
                if (newItem is SignalReference signalReference && !signalReference.TopLevelModule.Equals(Module))
                    throw new Exception($"Added signal reference must use module {Module} as top-level module");
                childEntities.Add(newItem);
            }

        updated?.Invoke(this, e);
    }
}