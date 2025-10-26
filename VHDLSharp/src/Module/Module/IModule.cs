using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Behaviors;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Utility;

namespace VHDLSharp.Modules;

/// <summary>
/// Interface for a digital module--a circuit that has some functionality
/// </summary>
public interface IModule : IEquatable<IModule>, ICompletable
{
    /// <summary>
    /// Name of the module
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Mapping of module signal to behavior that defines it
    /// </summary>
    public ObservableDictionary<INamedSignal, IBehavior> SignalBehaviors { get; }

    /// <summary>
    /// List of ports for this module
    /// </summary>
    public ObservableCollection<IPort> Ports { get; }

    /// <summary>
    /// Collection of module instantiations inside of this module
    /// </summary>
    public InstantiationCollection Instantiations { get; }

    /// <summary>
    /// Get all signals used in this module that belong to it. 
    /// Signals can come from ports, behavior input signals, assigned output signals, instantiations, or derived signals. 
    /// If all of a multi-dimensional signal's children are used, then the top-level signal is included. 
    /// Otherwise, only the used children are returned. 
    /// </summary>
    public IEnumerable<IModuleSpecificSignal> AllModuleSignals { get; }

    /// <summary>
    /// Get all modules used by this module as instantiations
    /// </summary>
    /// <param name="recursive">If true, returns modules used by used modules, etc.</param>
    /// <param name="compileDerivedSignals">If true, compiles derived signals into instantiations before including modules (and undoes it)</param>
    /// <returns></returns>
    public ISet<IModule> GetModulesUsed(bool recursive, bool compileDerivedSignals);

    /// <summary>
    /// Convert to string
    /// </summary>
    /// <returns></returns>
    public string ToString();

    /// <summary>
    /// Get the module as a VHDL string, including all modules used
    /// </summary>
    /// <returns></returns>
    public string GetVhdl();

    /// <summary>
    /// Get the VHDL for this module without submodules or 
    /// stuff that goes at the beginning of the file
    /// </summary>
    /// <returns></returns>
    public string GetVhdlNoSubmodules();

    /// <summary>
    /// Convert module to Spice circuit
    /// </summary>
    /// <returns></returns>
    public SpiceSubcircuit GetSpice();

    /// <summary>
    /// Convert module to Spice circuit given dictionary of modules that already have subcircuit definitions.
    /// This uses the given subcircuit definitions for applicable instances
    /// </summary>
    /// <param name="existingModuleLinkedSubcircuits">Set of all module-linked subcircuit definitions that already exist, so that this can point to one of those if applicable instead of making a new one</param>
    /// <returns></returns>
    public SpiceSubcircuit GetSpice(ISet<IModuleLinkedSubcircuitDefinition> existingModuleLinkedSubcircuits);

    /// <summary>
    /// Get simulation rules, using this module as the top level
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SimulationRule> GetSimulationRules();

    /// <summary>
    /// Get simulation rules, given a subcircuit reference that has this as the bottom level
    /// </summary>
    /// <param name="subcircuit"></param>
    /// <returns></returns>
    public IEnumerable<SimulationRule> GetSimulationRules(SubcircuitReference subcircuit);

    /// <summary>
    /// Test if the module contains a signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public bool ContainsSignal(IModuleSpecificSignal signal);

    /// <summary>
    /// Get VHDL component declaration for the module
    /// </summary>
    /// <returns></returns>
    public string GetVhdlComponentDeclaration();

    /// <summary>
    /// If this is a type of module that links to another module (ex. <see cref="ParameterizedModule{T}"/>),
    /// this should be a link to that module
    /// </summary>
    public IModule BaseModule => this;

    /// <summary>
    /// Removes instantiations and linked signals that were added during compilation,
    /// which happens whenever a code-production or simulation function is run
    /// </summary>
    public void UndoDerivedSignalCompilation();
}
