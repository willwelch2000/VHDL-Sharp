namespace VHDLSharp.Validation;

/// <summary>
/// A documented issue with an entity
/// </summary>
public class Issue()
{
    /// <summary>
    /// Reference-point entity
    /// </summary>
    public required IValidityManagedEntity TopLevelEntity { get; set; }

    /// <summary>
    /// Chain of children from the top-level entity down to the offending entity
    /// </summary>
    public LinkedList<IValidityManagedEntity> FaultChain { get; set; } = [];

    /// <summary>
    /// Explanation for why there is a problem
    /// </summary>
    public required string Explanation { get; set; }
}