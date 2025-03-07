using System.Diagnostics.CodeAnalysis;

namespace VHDLSharp.Validation;

/// <summary>
/// Interface for anything that can be used in a <see cref="ValidityManager"/>.
/// </summary>
public interface IValidityManagedEntity
{
    /// <summary>
    /// Event to be called when entity is updated. 
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
    /// Returns true if this entity is valid, not taking child entities into account
    /// </summary>
    /// <param name="explanation">Explanation for issue--can be null if returning true</param>
    /// <returns>True for valid, false for invalid</returns>
    public bool CheckTopLevelValidity([MaybeNullWhen(true)] out string explanation) // TODO consider making explanation an array--or an Exception that can be thrown
    {
        explanation = null;
        return true;
    }
}