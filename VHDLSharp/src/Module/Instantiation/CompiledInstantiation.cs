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
    internal CompiledInstantiation(IInstantiation baseInstantiation)
    {
        BaseInstantiation = baseInstantiation;
    }

    /// <inheritdoc/>
    public IInstantiation BaseInstantiation { get; }

    /// <inheritdoc/>
    public PortMapping PortMapping => BaseInstantiation.PortMapping;

    /// <inheritdoc/>
    public string Name => BaseInstantiation.Name;

    /// <inheritdoc/>
    public ValidityManager ValidityManager => BaseInstantiation.ValidityManager;

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules() => BaseInstantiation.GetSimulationRules();

    /// <inheritdoc/>
    public IEnumerable<SimulationRule> GetSimulationRules(SubmoduleReference submodule) => BaseInstantiation.GetSimulationRules(submodule);

    /// <inheritdoc/>
    public SpiceCircuit GetSpice(ISet<IModuleLinkedSubcircuitDefinition> existingModuleLinkedSubcircuits) => BaseInstantiation.GetSpice(existingModuleLinkedSubcircuits);

    /// <inheritdoc/>
    public string GetVhdlStatement() => BaseInstantiation.GetVhdlStatement();

    /// <inheritdoc/>
    public bool IsComplete([MaybeNullWhen(true)] out string reason) => BaseInstantiation.IsComplete(out reason);

    /// <inheritdoc/>
    public override string ToString() => BaseInstantiation.ToString();

    /// <inheritdoc/>
    public bool Equals(IInstantiation? other) => other is not null && other.BaseInstantiation == BaseInstantiation;
}