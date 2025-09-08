namespace VHDLSharp.Signals;

/// <summary>
/// Marker interface that signifies a single-node signal that has an assigned module
/// </summary>
public interface ISingleNodeModuleSpecificSignal : IModuleSpecificSignal, ISingleNodeSignal;