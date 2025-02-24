using System.Collections.ObjectModel;
using SpiceSharp;
using SpiceSharp.Components;
using VHDLSharp.Behaviors;
using VHDLSharp.Signals;
using VHDLSharp.Utility;

namespace VHDLSharp.Modules;

/// <summary>
/// Interface for a digital module--a circuit that has some functionality
/// </summary>
public interface IModule
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
    /// List of module instantiations inside of this module
    /// </summary>
    public ObservableCollection<IInstantiation> Instantiations { get; }

    /// <summary>
    /// Get all named signals used in this module. 
    /// Signals can come from ports, behavior input signals, or output signals. 
    /// If all of a multi-dimensional signal's children are used, then only the top-level signal should be included. 
    /// Otherwise, only the children should be returned. 
    /// </summary>
    public IEnumerable<INamedSignal> NamedSignals { get; }

    /// <summary>
    /// Get all modules (recursive) used by this module as instantiations
    /// </summary>
    public IEnumerable<IModule> ModulesUsed { get; }

    /// <summary>
    /// True if module is ready to be used
    /// </summary>
    public bool Complete { get; }

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
    public string GetSpice();

    /// <summary>
    /// Convert module to Spice circuit
    /// </summary>
    /// <param name="subcircuit">Whether it should be wrapped in a subcircuit or top-level</param>
    /// <returns></returns>
    public string GetSpice(bool subcircuit);

    /// <summary>
    /// Convert module to Spice# <see cref="SubcircuitDefinition"/> object
    /// </summary>
    /// <returns></returns>
    public SubcircuitDefinition GetSpiceSharpSubcircuit();

    /// <summary>
    /// Convert module to Spice# <see cref="Circuit"/> object
    /// </summary>
    /// <returns></returns>
    public Circuit GetSpiceSharpCircuit();

    /// <summary>
    /// Test if the module contains a signal
    /// </summary>
    /// <param name="signal"></param>
    /// <returns></returns>
    public bool ContainsSignal(INamedSignal signal);
}
