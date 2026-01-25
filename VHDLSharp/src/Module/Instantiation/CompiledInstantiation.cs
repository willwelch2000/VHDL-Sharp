using System.Diagnostics.CodeAnalysis;
using VHDLSharp.Signals;
using VHDLSharp.Simulations;
using VHDLSharp.SpiceCircuits;
using VHDLSharp.Validation;

namespace VHDLSharp.Modules;

/// <summary>
/// An instantiation that is created during derived signal compilation
/// </summary>
public class CompiledInstantiation : IInstantiation, ICompiledObject
{
    private readonly IInstantiation baseInstantiation;
    internal CompiledInstantiation(IInstantiation baseInstantiation)
    {
        this.baseInstantiation = baseInstantiation;
    }

    /// <inheritdoc/>
    public PortMapping PortMapping => baseInstantiation.PortMapping;

    /// <inheritdoc/>
    public string Name => baseInstantiation.Name;

    /// <inheritdoc/>
    public ValidityManager ValidityManager => baseInstantiation.ValidityManager;

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules() => baseInstantiation.GetSimulationRules();

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules(SubmoduleReference submodule) => baseInstantiation.GetSimulationRules(submodule);

    /// <inheritdoc/>
    public SpiceCircuit GetSpice(ISet<IModuleLinkedSubcircuitDefinition> existingModuleLinkedSubcircuits) => baseInstantiation.GetSpice(existingModuleLinkedSubcircuits);

    /// <inheritdoc/>
    public string GetVhdlStatement() => baseInstantiation.GetVhdlStatement();

    /// <inheritdoc/>
    public bool IsComplete([MaybeNullWhen(true)] out string reason) => baseInstantiation.IsComplete(out reason);

    /// <inheritdoc/>
    public override string ToString() => baseInstantiation.ToString();
}