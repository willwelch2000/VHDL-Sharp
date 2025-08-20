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

    ISingleNodeSignal ISignal.this[int index] => index == 0 ? this :
        throw new ArgumentOutOfRangeException(nameof(index), $"Must be 0 for single node signal");

    IEnumerable<ISignal> ISignal.ChildSignals => [];

    IEnumerable<ISingleNodeSignal> ISignal.ToSingleNodeSignals => ToSingleNodeSignals;
}