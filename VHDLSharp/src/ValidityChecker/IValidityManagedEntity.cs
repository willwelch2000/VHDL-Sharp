using System.Diagnostics.CodeAnalysis;

namespace VHDLSharp.Validation;

/// <summary>
/// Interface for anything that can be used in a <see cref="ValidityManager"/>.
/// </summary>
public interface IValidityManagedEntity
{
    /// <summary>
    /// Event to be called when entity is directly updated--not when a child is updated. 
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
    /// <param name="exception">Exceptioin to throw for issue--can be null if returning true</param>
    /// <returns>True for valid, false for invalid</returns>
    public bool CheckTopLevelValidity([MaybeNullWhen(true)] out Exception exception) // TODO consider making exception an array
    {
        exception = null;
        return true;
    }
}