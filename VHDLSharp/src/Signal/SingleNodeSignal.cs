using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// Base class for any signal that contains just a single node (not a vector)
/// </summary>
public abstract class SingleNodeSignal : ISignal
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract Module Parent { get; }

    /// <inheritdoc/>
    public int Dimension => 1;

    /// <inheritdoc/>
    public string VhdlType => "std_logic";

    /// <inheritdoc/>
    public string ToVhdl => $"signal {Name}\t: {VhdlType}";

    /// <inheritdoc/>
    public IEnumerable<ISignal> BaseObjects => [this];

    /// <inheritdoc/>
    public bool CanCombine(ILogicallyCombinable<ISignal> other)
    {
        ISignal? signal = other.BaseObjects.FirstOrDefault();
        if (signal is null)
            return true;
        return Dimension == signal.Dimension && Parent == signal.Parent;
    }

    /// <inheritdoc/>
    public string ToLogicString() => Name;
}