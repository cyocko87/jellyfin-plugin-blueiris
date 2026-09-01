using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Gets a simplified camera list with stream and snapshot URLs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Simplified list of cameras.</returns>
    [HttpGet("CameraList")]
    public async Task<IActionResult> CameraList(CancellationToken cancellationToken = default)
    {
        var config = ApplyConfig();
        var cameras = await _client.GetCamerasAsync(cancellationToken).ConfigureAwait(false);

        var streamType = string.Equals(config.StreamType, "mjpeg", StringComparison.OrdinalIgnoreCase)
            ? "MJPEG"
            : "HLS";

        var list = cameras
            .Select(camera => new
            {
                camera.ShortName,
                camera.DisplayName,
                SnapshotUrl = _client.BuildSnapshotUrl(camera.ShortName),
                StreamUrl = _client.BuildStreamUrl(camera.ShortName, streamType)
            })
            .ToList();

        return Ok(list);
    }

    /// <summary>
    /// Gets a live JPEG snapshot for a camera.
    /// </summary>
    /// <param name="camera">Camera short name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JPEG snapshot image.</returns>
    [HttpGet("Snapshot")]
    public async Task<IActionResult> GetSnapshot([FromQuery] string camera, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(camera))
        {
            return BadRequest(new { message = "Camera short name is required." });
        }

        ApplyConfig();

        try
        {
            var bytes = await _client.GetSnapshotAsync(camera, cancellationToken).ConfigureAwait(false);
            return File(bytes, "image/jpeg");
        }
        catch (Exception ex)
        {
            return NotFound(new { message = $"Could not retrieve snapshot for '{camera}': {ex.Message}" });
        }
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

    private PluginConfiguration ApplyConfig()
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        _client.BaseUrl = config.ServerUrl;
        _client.Username = config.Username;
        _client.Password = config.Password;
        return config;
    }
}
