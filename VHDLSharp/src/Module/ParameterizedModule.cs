using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Behaviors;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;

namespace VHDLSharp.Modules;

/// <summary>
/// Abstract class that allows a "parameterized" module, where an input object defines the module's parameters. 
/// Every time that a parameterized module is instantiated using the same parameter set, the same backend 
/// module will be used. 
/// A class inheriting this must provide a function, <see cref="BuildModule"/>, that constructs a module given the input
/// </summary>
/// <typeparam name="T">The input object type. Must implement <see cref="IEquatable{T}"/></typeparam>
public abstract class ParameterizedModule<T> : IModule where T : notnull, IEquatable<T>
{
    /// <summary>Dictionary that tracks all modules given the input</summary>
    private static readonly Dictionary<T, IModule> allModules = [];

    /// <inheritdoc/>
    public IModule BaseModule { get; }

    /// <summary>
    /// Constructor given input object
    /// </summary>
    /// <param name="options"></param>
    public ParameterizedModule(T options)
    {
        if (allModules.TryGetValue(options, out IModule? foundModule))
            BaseModule = foundModule;
        else
        {
            BaseModule = BuildModule(options);
            allModules.Add(options, BaseModule);
        }
    }

    /// <summary>
    /// Function to build the module given the input options object
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public abstract IModule BuildModule(T options);

    /// <inheritdoc/>
    public string Name => BaseModule.Name;

    /// <inheritdoc/>
    public ObservableDictionary<INamedSignal, IBehavior> SignalBehaviors => BaseModule.SignalBehaviors;

    /// <inheritdoc/>
    public ObservableCollection<IPort> Ports => BaseModule.Ports;

    /// <inheritdoc/>
    public InstantiationCollection Instantiations => BaseModule.Instantiations;

    /// <inheritdoc/>
    public IEnumerable<IModuleSpecificSignal> AllModuleSignals => BaseModule.AllModuleSignals;

    /// <inheritdoc/>
    public ISet<IModule> GetModulesUsed(bool recursive, bool compileDerivedSignals) => BaseModule.GetModulesUsed(recursive, compileDerivedSignals);

    /// <inheritdoc/>
    public bool ContainsSignal(IModuleSpecificSignal signal) => BaseModule.ContainsSignal(signal);

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules() => BaseModule.GetSimulationRules();

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules(SubcircuitReference subcircuit) => BaseModule.GetSimulationRules(subcircuit);

    /// <inheritdoc/>
    public SpiceSubcircuit GetSpice() => BaseModule.GetSpice();

    /// <inheritdoc/>
    public SpiceSubcircuit GetSpice(ISet<IModuleLinkedSubcircuitDefinition> existingModuleLinkedSubcircuits) => BaseModule.GetSpice(existingModuleLinkedSubcircuits);

    /// <inheritdoc/>
    public override string ToString() => BaseModule.ToString();

    /// <inheritdoc/>
    public string GetVhdl() => BaseModule.GetVhdl();

    /// <inheritdoc/>
    public string GetVhdlComponentDeclaration() => BaseModule.GetVhdlComponentDeclaration();

    /// <inheritdoc/>
    public string GetVhdlNoSubmodules() => BaseModule.GetVhdlNoSubmodules();

    /// <inheritdoc/>
    public bool IsComplete([MaybeNullWhen(true)] out string reason) => BaseModule.IsComplete(out reason);

    /// <inheritdoc/>
    public override int GetHashCode() => BaseModule.GetHashCode();

    /// <inheritdoc/>
    public bool Equals(IModule? other) => other is not null && other.BaseModule == BaseModule;

    /// <inheritdoc/>
    public void UndoDerivedSignalCompilation() => BaseModule.UndoDerivedSignalCompilation();

    /// <inheritdoc/>
    public void RegisterDerivedSignal(IDerivedSignal signal) => BaseModule.RegisterDerivedSignal(signal);
}