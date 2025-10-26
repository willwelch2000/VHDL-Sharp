using System.Diagnostics.CodeAnalysis;

namespace VHDLSharp;

/// <summary>
/// Something that can be complete or incomplete
/// </summary>
public interface ICompletable
{
    /// <summary>
    /// Check if the object is complete
    /// </summary>
    /// <param name="reason">Explanation for why it's not complete</param>
    /// <returns>True if complete, false otherwise</returns>
    public bool IsComplete([MaybeNullWhen(true)] out string reason);
}