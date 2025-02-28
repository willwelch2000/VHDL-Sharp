namespace VHDLSharp.Validation;

/// <summary>
/// Interface for anything that can be used in a <see cref="ValidityManager"/>.
/// It is assumed that anything implementing this validates itself for the information it has.
/// In other words, if there is no exception thrown by this object, it should be guaranteed to be valid
/// </summary>
public interface IValidityManagedEntity
{
    /// <summary>
    /// Event called when entity is updated.
    /// In general, implementing classes should reject changes that cause an exception when this event is called.
    /// A reasonable strategy is to wrap the invocation in a try-catch that undoes the action and rethrows the exception
    /// </summary>
    public event EventHandler? Updated
    {
        add {}
        remove {}
    }

    /// <summary>
    /// Linked validity manager object--should be created during construction
    /// </summary>
    public ValidityManager ValidityManager { get; }

    /// <summary>
    /// Function called when validation is necessary.
    /// Called when entity or child entity is updated. 
    /// Should raise Exception if there's a problem
    /// </summary>
    public void CheckValidity() {}
}