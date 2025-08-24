using VHDLSharp.LogicTree;
using VHDLSharp.Modules;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for a node of a derived signal. 
/// These signals are handled differently because their parent <see cref="DerivedSignal"/> must be compiled.
/// Additionally, the parent must be considered when recursively finding all signals used. 
/// </summary>
public interface IDerivedSignalNode : ISingleNodeSignal, ISignalWithAssignedModule
{
    /// <summary>Parent <see cref="IDerivedSignal"/> that this belongs to</summary>
    public IDerivedSignal DerivedSignal { get; }
}

/// <summary>
/// Single node in a <see cref="IDerivedSignal"/>
/// </summary>
/// <param name="derivedSignal">Parent derived signal</param>
/// <param name="node">Index in the parent</param>
public class DerivedSignalNode(IDerivedSignal derivedSignal, int node) : IDerivedSignalNode, IEquatable<DerivedSignalNode>
{
    /// <inheritdoc/>
    public IDerivedSignal DerivedSignal { get; } = derivedSignal;

    /// <summary>The index in the <see cref="DerivedSignal"/></summary>
    public int Node { get; } = node;

    /// <summary>Linked signal, derived by getting the node of the parent <see cref="DerivedSignal"/>'s linked signal</summary>
    public ISingleNodeNamedSignal? LinkedSignal => DerivedSignal.LinkedSignal is INamedSignal namedSignal ? namedSignal[Node] : null;

    /// <inheritdoc/>
    public ISignal? ParentSignal => DerivedSignal;

    /// <inheritdoc/>
    public IModule ParentModule => DerivedSignal.ParentModule;

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other) => ISignal.CanCombineSignals(this, other);

    /// <inheritdoc/>
    public string GetSpiceName() => LinkedSignal is null ?
        throw new Exception("Must have a linked assigned signal to get the Spice name") : LinkedSignal.GetSpiceName();

    /// <inheritdoc/>
    public string GetVhdlName() => LinkedSignal is null ?
        throw new Exception("Must have a linked assigned signal to get the VHDL name") : LinkedSignal.GetVhdlName();

    /// <inheritdoc/>
    public string ToLogicString() => LinkedSignal is null ?
        throw new Exception("Must have a linked assigned signal to get the logic string") : LinkedSignal.ToLogicString();

    /// <inheritdoc/>
    public string ToLogicString(LogicStringOptions options) => LinkedSignal is null ?
        throw new Exception("Must have a linked assigned signal to get the logic string") : LinkedSignal.ToLogicString(options);

    /// <inheritdoc/>
    public bool Equals(DerivedSignalNode? other) =>
        other is not null && other.DerivedSignal == DerivedSignal && other.Node == Node;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as DerivedSignalNode);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode code = new();
        code.Add(DerivedSignal);
        code.Add(Node);
        return code.ToHashCode();
    }
}