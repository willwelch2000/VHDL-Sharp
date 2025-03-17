using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SpiceSharp.Components;
using VHDLSharp.Signals;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// Collection of instantiations that simplifies Spice and Spice# conversion for groups of instantiations
/// </summary>
public class InstantiationCollection : ICollection<IInstantiation>, IValidityManagedEntity
{
    private readonly ObservableCollection<IInstantiation> instantiations;

    private readonly ValidityManager validityManager;

    private EventHandler? updated;

    internal InstantiationCollection(IModule parentModule)
    {
        ParentModule = parentModule;
        instantiations = [];
        validityManager = new ValidityManager<IInstantiation>(this, instantiations);
        instantiations.CollectionChanged += InstantiationsListUpdated;
    }

    /// <summary>
    /// Parent module to which collection belongs
    /// </summary>
    public IModule ParentModule { get; }

    ValidityManager IValidityManagedEntity.ValidityManager => validityManager;

    /// <inheritdoc/>
    public int Count => instantiations.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    event EventHandler? IValidityManagedEntity.Updated
    {
        add
        {
            updated -= value; // remove if already present
            updated += value;
        }
        remove => updated -= value;
    }

    bool IValidityManagedEntity.CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception)
    {
        exception = null;

        // Don't allow duplicate instantiation names
        HashSet<string> instantiationNames = [];
        if (!instantiations.All(i => instantiationNames.Add(i.Name)))
        {
            string duplicate = instantiations.First(i => instantiations.Count(i2 => i.Name == i2.Name) > 1).Name;
            exception = new Exception($"More than one instantiation with name \"{duplicate}\"");
        }

        // Don't allow duplicate instantiated module names
        HashSet<string> moduleNames = [];
        IModule[] instantiatedModules = [.. instantiations.Select(i => i.InstantiatedModule).Distinct()];
        if (!instantiatedModules.All(m => moduleNames.Add(m.Name)))
        {
            string duplicate = instantiatedModules.First(m => instantiatedModules.Count(m2 => m.Name == m2.Name) > 1).Name;
            exception = new Exception($"More than one instantiated moodule with name \"{duplicate}\"");
        }

        // Must have correct parent module
        if (instantiations.Any(i => i.ParentModule != ParentModule))
        {
            IInstantiation incorrect = instantiations.First(i => i.ParentModule != ParentModule);
            exception = new Exception($"Instantiations must have correct parent module ({ParentModule})");
        }

        return exception is null;
    }

    /// <summary>
    /// Get Spice circuit representing the collection of instantiations
    /// </summary>
    /// <returns></returns>
    public SpiceCircuit GetSpice()
    {
        if (!validityManager.IsValid())
            throw new InvalidException("Instantiation collection is invalid");

        // TODO NEXT
        // Make subcircuit definitions for all distinct modules
        Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions = [];
        Dictionary<ISubcircuitDefinition, string> subcircuitNames = [];
        foreach (IModule submodule in instantiations.Select(i => i.InstantiatedModule).Distinct())
        {
            SubcircuitDefinition subcircuitDefinition = submodule.GetSpice().AsSubcircuit();
            subcircuitDefinitions[submodule] = subcircuitDefinition;
        }

        // Combine all instantiations into one circuit
        return SpiceCircuit.Combine(instantiations.Select(i => i.GetSpice(subcircuitDefinitions)));
    }

    /// <summary>
    /// Get all signals that are a given direction for the instantiations
    /// </summary>
    /// <param name="direction">Direction for signals to instantiations</param>
    /// <returns></returns>
    public IEnumerable<INamedSignal> GetSignals(PortDirection direction) => this.SelectMany(i => i.GetSignals(direction)).Distinct();

    /// <summary>
    /// Gets VHDL for all instantiations, appended together.
    /// Manages adding extra new line at end, if there's any
    /// </summary>
    /// <returns></returns>
    public string GetVhdl()
    {
        if (!instantiations.Any())
            return "";
        
        StringBuilder sb = new();
        foreach (IInstantiation instantiation in instantiations)
            sb.AppendLine(instantiation.GetVhdlStatement());
        sb.AppendLine();

        return sb.ToString();
    }

    /// <inheritdoc/>
    public void Add(IInstantiation instantiation) => instantiations.Add(instantiation);

    /// <summary>
    /// Add new instantiation automatically
    /// </summary>
    /// <param name="module">Module to be instantiated</param>
    /// <param name="name">Name of instantiation</param>
    /// <returns></returns>
    public Instantiation Add(IModule module, string name)
    {
        Instantiation instantiation = new(module, ParentModule, name);
        Add(instantiation);
        return instantiation;
    }

    /// <inheritdoc/>
    public void Clear() => instantiations.Clear();

    /// <inheritdoc/>
    public bool Contains(IInstantiation instantiation) => instantiations.Contains(instantiation);

    /// <inheritdoc/>
    public void CopyTo(IInstantiation[] array, int arrayIndex) => instantiations.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(IInstantiation instantiation) => instantiations.Remove(instantiation);

    /// <inheritdoc/>
    public IEnumerator<IInstantiation> GetEnumerator() => instantiations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void InstantiationsListUpdated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        updated?.Invoke(this, e);
    }
}