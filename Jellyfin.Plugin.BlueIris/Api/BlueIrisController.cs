using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.BlueIris.BlueIris;
using Jellyfin.Plugin.BlueIris.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.BlueIris.Api;

/// <summary>
/// Exposes Blue Iris cameras and stream URLs under /BlueIris.
/// </summary>
[ApiController]
[Route("BlueIris")]
[Authorize]
public class BlueIrisController : ControllerBase
{
    private readonly BlueIrisClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlueIrisController"/> class.
    /// </summary>
    /// <param name="client">The Blue Iris client.</param>
    public BlueIrisController(BlueIrisClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Gets the list of cameras.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cameras.</returns>
    [HttpGet("Cameras")]
    public async Task<ActionResult<IReadOnlyList<CameraInfo>>> GetCameras(CancellationToken cancellationToken = default)
    {
        ApplyConfig();
        var cameras = await _client.GetCamerasAsync(cancellationToken).ConfigureAwait(false);
        return Ok(cameras);
    }

    /// <summary>
    /// Builds a stream URL for a camera.
    /// </summary>
    /// <param name="camera">Camera short name.</param>
    /// <param name="type">Stream type: HLS or MJPEG.</param>
    /// <returns>Stream URL.</returns>
    [HttpGet("Stream")]
    public ActionResult<string> GetStream([FromQuery] string camera, [FromQuery] string type)
    {
        ApplyConfig();
        return _client.BuildStreamUrl(camera, type);
    }

    private void ApplyConfig()
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        _client.BaseUrl = config.ServerUrl;
        _client.Username = config.Username;
        _client.Password = config.Password;
    }
}
