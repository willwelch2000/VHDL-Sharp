using System.Collections.ObjectModel;
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

    private readonly IModule module;

    /// <summary>
    /// Constructor given input object
    /// </summary>
    /// <param name="options"></param>
    public ParameterizedModule(T options)
    {
        if (allModules.TryGetValue(options, out IModule? foundModule))
            module = foundModule;
        else
        {
            module = BuildModule(options);
            allModules.Add(options, module);
        }
    }

    /// <summary>
    /// Function to build the module given the input object
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public abstract IModule BuildModule(T input);

    /// <inheritdoc/>
    public string Name => module.Name;

    /// <inheritdoc/>
    public ObservableDictionary<INamedSignal, IBehavior> SignalBehaviors => module.SignalBehaviors;

    /// <inheritdoc/>
    public ObservableCollection<IPort> Ports => module.Ports;

    /// <inheritdoc/>
    public InstantiationCollection Instantiations => module.Instantiations;

    /// <inheritdoc/>
    public IEnumerable<INamedSignal> NamedSignals => module.NamedSignals;

    /// <inheritdoc/>
    public IEnumerable<IModule> ModulesUsed => module.ModulesUsed;

    /// <inheritdoc/>
    public bool ContainsSignal(INamedSignal signal) => module.ContainsSignal(signal);

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules() => module.GetSimulationRules();

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules(SubcircuitReference subcircuit) => module.GetSimulationRules(subcircuit);

    /// <inheritdoc/>
    public SpiceSubcircuit GetSpice() => module.GetSpice();

    /// <inheritdoc/>
    public SpiceSubcircuit GetSpice(ISet<IModuleLinkedSubcircuitDefinition> existingModuleLinkedSubcircuits) => module.GetSpice(existingModuleLinkedSubcircuits);

    /// <inheritdoc/>
    public override string ToString() => module.ToString();

    /// <inheritdoc/>
    public string GetVhdl() => module.GetVhdl();

    /// <inheritdoc/>
    public string GetVhdlComponentDeclaration() => module.GetVhdlComponentDeclaration();

    /// <inheritdoc/>
    public string GetVhdlNoSubmodules() => module.GetVhdlNoSubmodules();

    /// <inheritdoc/>
    public bool IsComplete() => module.IsComplete();

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ParameterizedModule<T> paramMod &&
        paramMod.module.Equals(module);

    /// <inheritdoc/>
    public override int GetHashCode() => module.GetHashCode();
}