namespace VHDLSharp.Validation;

/// <summary>
/// Interface for anything that can be used in a <see cref="ValidityManager"/>.
/// It is assumed that anything implementing this validates itself for the information it has
/// </summary>
public interface IValidityManagedEntity
{
    /// <summary>
    /// Event called when entity is updated
    /// </summary>
    public event EventHandler? Updated;

    /// <summary>
    /// Linked validity manager object--should be created during construction
    /// </summary>
    public ValidityManager ValidityManager { get; }

    /// <summary>
    /// Function called when validation is necessary. 
    /// Should raise Exception if there's a problem
    /// </summary>
    public void CheckValidity();
}