// This controller is a stub. It requires the Jellyfin.Api NuGet package (10.9.11 or later)
// to compile. Because Jellyfin.Api is not referenced by this project, the file is excluded
// from the build via <Compile Remove="Api\BlueIrisController.cs" /> in the .csproj.
//
// To enable it: add <PackageReference Include="Jellyfin.Api" Version="10.9.11" /> to the
// main project, remove the <Compile Remove /> line, and ensure the controller is registered
// with Jellyfin's dependency injection.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Plugin.BlueIris.BlueIris;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.BlueIris.Api;

/// <summary>
/// Exposes Blue Iris cameras and stream URLs under /BlueIris.
/// </summary>
[Route("BlueIris")]
[Authorize]
public class BlueIrisController : BaseJellyfinApiController
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
        return _client.BuildStreamUrl(camera, type);
    }
}
