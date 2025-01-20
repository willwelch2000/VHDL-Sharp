using VHDLSharp.Modules;

namespace VHDLSharp;

/// <summary>
/// Interface defining anything that belongs to a module
/// </summary>
public interface IHasParentModule
{
    /// <summary>
    /// Module to which this object belongs
    /// </summary>
    public Module? ParentModule { get; }
}