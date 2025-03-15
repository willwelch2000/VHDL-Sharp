using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using SpiceSharp.Components;
using SpiceSharp.Entities;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;
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

        // Must have correct parent module
        if (instantiations.Any(i => i.ParentModule != ParentModule))
        {
            IInstantiation incorrect = instantiations.First(i => i.ParentModule != ParentModule);
            exception = new Exception($"Instantiations must have correct parent module ({ParentModule})");
        }

        return exception is null;
    }

    // /// <summary>
    // /// Get list of instantiations as list of entities for Spice#
    // /// </summary>
    // public IEnumerable<IEntity> GetSpiceSharpEntities()
    // {
    //     // Make subcircuit definitions for all distinct modules
    //     Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions = [];
    //     foreach (IModule submodule in instantiations.Select(i => i.InstantiatedModule).Distinct())
    //         subcircuitDefinitions[submodule] = submodule.GetSpiceSharpSubcircuit();

    //     // Add instantiations
    //     foreach (IInstantiation instantiation in instantiations)
    //         yield return instantiation.GetSpiceSharpSubcircuit(subcircuitDefinitions);
    // }

    /// <summary>
    /// Get Spice circuit representing the collection of instantiations
    /// </summary>
    /// <returns></returns>
    public SpiceCircuit GetSpice()
    {
        // Make subcircuit definitions for all distinct modules
        Dictionary<IModule, SubcircuitDefinition> subcircuitDefinitions = [];
        Dictionary<ISubcircuitDefinition, string> subcircuitNames = [];
        foreach (IModule submodule in instantiations.Select(i => i.InstantiatedModule).Distinct())
        {
            SubcircuitDefinition subcircuitDefinition = submodule.GetSpice().AsSubcircuit();
            subcircuitDefinitions[submodule] = subcircuitDefinition;
            subcircuitNames[subcircuitDefinition] = submodule.Name;
        }

        // Combine all instantiations into one circuit
        SpiceCircuit circuit = SpiceCircuit.Combine(instantiations.Select(i => i.GetSpice(subcircuitDefinitions)));
        circuit.SubcircuitNames = subcircuitNames;
        return circuit.WithCommonEntities();
    }

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

    // /// <summary>
    // /// Get all instantiated subcircuits' Spice declarations, as enumerable of strings
    // /// </summary>
    // /// <returns></returns>
    // public IEnumerable<string> GetSpiceSubcircuitDeclarations()
    // {
    //     // Add all inner modules' subcircuit declarations--no recursion needed here because the inner modules' inner modules are declared in the subcircuit
    //     foreach (IModule submodule in instantiations.Select(i => i.InstantiatedModule).Distinct())
    //         yield return submodule.GetSpice(true) + "\n";
    // }

    /// <summary>
    /// Get all instantiations as Spice instance statements, together in a string
    /// </summary>
    /// <returns></returns>
    public string GetSpiceInstantiationStatements()
    {
        if (!instantiations.Any())
            return "";

        StringBuilder sb = new();
        // foreach (IInstantiation instantiation in instantiations)
        //     sb.AppendLine(instantiation.GetSpice());
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
    public IInstantiation Add(Module module, string name)
    {
        IInstantiation instantiation = new Instantiation(module, ParentModule, name);
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