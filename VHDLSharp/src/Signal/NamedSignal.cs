using VHDLSharp.LogicTree;

namespace VHDLSharp;

/// <summary>
/// Single-node and vector signals that are contained in a module and have a name
/// </summary>
public abstract class NamedSignal : IBaseSignal
{
    /// <summary>
    /// Name of the module the signal is in
    /// </summary>
    public abstract Module Parent { get; }

    /// <summary>
    /// Name of the signal
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Type of signal as VHDL
    /// </summary>
    public abstract string VhdlType { get; }

    /// <summary>
    /// Get signal as VHDL
    /// TODO might should be renamed
    /// </summary>
    /// <returns></returns>
    public abstract string ToVhdl { get; }

    /// <summary>
    /// Dimension of signal
    /// Of type <see cref="Dimension"/>
    /// </summary>
    public Dimension Dimension => DefiniteDimension;

    /// <summary>
    /// Dimension of signal with definite value
    /// Of type <see cref="DefiniteDimension"/>
    /// </summary>
    public abstract DefiniteDimension DefiniteDimension { get; }

    /// <inheritdoc/>
    public abstract IEnumerable<IBaseSignal> BaseObjects { get; }

    /// <inheritdoc/>
    public abstract bool CanCombine(ILogicallyCombinable<IBaseSignal> other);

    /// <inheritdoc/>
    public abstract string ToLogicString();

    /// <inheritdoc/>
    public abstract string ToLogicString(LogicStringOptions options);

    /// <inheritdoc/>
    public abstract string ToVhdlInExpression(DefiniteDimension dimension);
}