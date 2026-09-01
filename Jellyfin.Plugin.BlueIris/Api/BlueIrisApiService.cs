using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.BlueIris.BlueIris;
using Jellyfin.Plugin.BlueIris.Configuration;

namespace Jellyfin.Plugin.BlueIris.Api;

/// <summary>
/// Service that exposes Blue Iris camera and stream operations to the plugin.
/// </summary>
public class BlueIrisApiService
{
    private readonly BlueIrisClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlueIrisApiService"/> class.
    /// </summary>
    /// <param name="configuration">Optional plugin configuration.</param>
    /// <param name="httpClient">Optional HttpClient for testing.</param>
    public BlueIrisApiService(PluginConfiguration? configuration = null, HttpClient? httpClient = null)
    {
        _client = new BlueIrisClient(httpClient);
        if (configuration != null)
        {
            ApplyConfiguration(configuration);
        }
    }

    /// <summary>
    /// Updates the Blue Iris configuration used by the service.
    /// </summary>
    /// <param name="configuration">Plugin configuration.</param>
    public void UpdateConfiguration(PluginConfiguration configuration)
    {
        ApplyConfiguration(configuration);
    }

    /// <summary>
    /// Gets the list of cameras from Blue Iris.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cameras.</returns>
    public Task<IReadOnlyList<CameraInfo>> GetCamerasAsync(CancellationToken cancellationToken = default)
        => _client.GetCamerasAsync(cancellationToken);

    /// <summary>
    /// Builds a stream URL for a camera and stream type.
    /// </summary>
    /// <param name="camera">Camera short name.</param>
    /// <param name="type">Stream type: HLS or MJPEG.</param>
    /// <returns>Stream URL.</returns>
    public string GetStreamUrl(string camera, string type)
        => _client.BuildStreamUrl(camera, type);

    /// <summary>
    /// Builds a snapshot URL for a camera.
    /// </summary>
    /// <param name="camera">Camera short name.</param>
    /// <returns>Snapshot URL.</returns>
    public string GetSnapshotUrl(string camera)
        => _client.BuildSnapshotUrl(camera);

    /// <summary>
    /// Fetches a snapshot image for a camera.
    /// </summary>
    /// <param name="camera">Camera short name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JPEG image bytes.</returns>
    public Task<byte[]> GetSnapshotAsync(string camera, CancellationToken cancellationToken = default)
        => _client.GetSnapshotAsync(camera, cancellationToken);

    private void ApplyConfiguration(PluginConfiguration configuration)
    {
        _client.BaseUrl = configuration.ServerUrl;
        _client.Username = configuration.Username;
        _client.Password = configuration.Password;
    }
}
