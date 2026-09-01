namespace Jellyfin.Plugin.BlueIris.BlueIris;

/// <summary>
/// Describes a Blue Iris camera.
/// </summary>
public class CameraInfo
{
    /// <summary>
    /// Gets or sets the short name used in Blue Iris URLs.
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human readable display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the camera group, if any.
    /// </summary>
    public string Group { get; set; } = string.Empty;
}
