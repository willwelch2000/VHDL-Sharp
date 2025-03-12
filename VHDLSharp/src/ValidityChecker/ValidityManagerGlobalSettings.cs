namespace VHDLSharp.Validation;

/// <summary>
/// Class defining settings for validity management globally.
/// This is used as a static property in <see cref="ValidityManager"/>, meaning that it defines behavior for all instances.
/// </summary>
public class ValidityManagerGlobalSettings
{
    private MonitorMode monitorMode = MonitorMode.Inactive;

    private readonly Action updated;

    internal ValidityManagerGlobalSettings(Action updated)
    {
        this.updated = updated;
    }

    /// <summary>
    /// Monitoring mode that controls if changes raise alerts on <see cref="ValidityManager.ChangeDetectedInMainOrTrackedEntity"/> and/or throw exceptions if invalid
    /// </summary>
    public MonitorMode MonitorMode
    {
        get => monitorMode;
        set
        {
            monitorMode = value;
            updated();
        }
    }
}