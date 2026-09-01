using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BlueIris.Configuration;

/// <summary>
/// Plugin configuration for Blue Iris.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ServerUrl = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        AllowedCameras = System.Array.Empty<string>();
        StreamType = "HLS";
        RestrictToAdmins = true;
    }

    /// <summary>
    /// Gets or sets the Blue Iris server URL, e.g. http://192.168.5.5:81.
    /// </summary>
    public string ServerUrl { get; set; }

    /// <summary>
    /// Gets or sets the Blue Iris username.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the Blue Iris password.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets the list of allowed camera short names.
    /// </summary>
    public string[] AllowedCameras { get; set; }

    /// <summary>
    /// Gets or sets the preferred stream type: HLS or MJPEG.
    /// </summary>
    public string StreamType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cameras page is visible to administrators only.
    /// </summary>
    public bool RestrictToAdmins { get; set; }
}
