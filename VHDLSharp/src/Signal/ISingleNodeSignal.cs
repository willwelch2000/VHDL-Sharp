using VHDLSharp.Dimensions;

namespace VHDLSharp.Signals;

/// <summary>
/// Interface for signal of any type that has exactly one node. 
/// Dimension must be 1
/// </summary>
public interface ISingleNodeSignal : ISignal
{
    /// <summary>
    /// Get representation in SPICE
    /// </summary>
    /// <returns></returns>
    public string GetSpiceName();

    DefiniteDimension ISignal.Dimension => new(1);

    ISingleNodeSignal ISignal.this[Index index] => (index.IsFromEnd ? 1 - index.Value : index.Value) == 0 ? this :
        throw new ArgumentOutOfRangeException(nameof(index), $"Must refer to node 0 for single node signal");

    IEnumerable<ISignal> ISignal.ChildSignals => [];

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;
}