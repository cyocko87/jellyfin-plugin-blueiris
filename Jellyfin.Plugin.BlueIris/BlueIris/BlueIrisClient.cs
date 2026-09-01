using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.BlueIris.BlueIris;

/// <summary>
/// Client for the Blue Iris HTTP/JSON and media endpoints.
/// </summary>
public class BlueIrisClient
{
    private readonly HttpClient _client;
    private string? _session;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlueIrisClient"/> class.
    /// </summary>
    /// <param name="httpClient">Optional HttpClient for testing or injection.</param>
    public BlueIrisClient(HttpClient? httpClient = null)
    {
        _client = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlueIrisClient"/> class with a base URL.
    /// </summary>
    /// <param name="baseUrl">The Blue Iris server base URL.</param>
    /// <param name="httpClient">Optional HttpClient for testing or injection.</param>
    public BlueIrisClient(string baseUrl, HttpClient? httpClient = null)
        : this(httpClient)
    {
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// Gets or sets the Blue Iris server base URL.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Blue Iris username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Blue Iris password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Fetches the camera list from Blue Iris using the camlist command.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cameras.</returns>
    public async Task<IReadOnlyList<CameraInfo>> GetCamerasAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
        {
            await LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        var payload = new Dictionary<string, object?>
        {
            ["cmd"] = "camlist"
        };

        if (!string.IsNullOrEmpty(_session))
        {
            payload["session"] = _session;
        }

        using var response = await PostJsonAsync(payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var cameras = new List<CameraInfo>();

        if (document.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                var shortName = GetString(item, "optionValue") ?? string.Empty;
                var display = GetString(item, "optionDisplay") ?? shortName;
                var group = GetString(item, "group") ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(shortName))
                {
                    cameras.Add(new CameraInfo
                    {
                        ShortName = shortName,
                        DisplayName = display,
                        Group = group
                    });
                }
            }
        }

        return cameras;
    }

    /// <summary>
    /// Builds a stream URL for a camera and stream type.
    /// </summary>
    /// <param name="cameraShortName">The Blue Iris camera short name.</param>
    /// <param name="type">Stream type: HLS or MJPEG.</param>
    /// <returns>The absolute stream URL.</returns>
    public string BuildStreamUrl(string cameraShortName, string type)
    {
        var path = string.Equals(type, "mjpeg", StringComparison.OrdinalIgnoreCase)
            ? $"/mjpg/{cameraShortName}/video.mjpg"
            : $"/h264/{cameraShortName}/temp.m3u8";

        var builder = new StringBuilder(BaseUrl.TrimEnd('/')).Append(path);

        if (!string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Password))
        {
            builder
                .Append('?')
                .Append("user=")
                .Append(Uri.EscapeDataString(Username))
                .Append("&pw=")
                .Append(Uri.EscapeDataString(Password));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds a snapshot URL for a camera.
    /// </summary>
    /// <param name="cameraShortName">The Blue Iris camera short name.</param>
    /// <param name="quality">JPEG quality.</param>
    /// <param name="scale">Image scale.</param>
    /// <returns>The absolute snapshot URL.</returns>
    public string BuildSnapshotUrl(string cameraShortName, int quality = 50, int scale = 80)
    {
        var builder = new StringBuilder(BaseUrl.TrimEnd('/'))
            .Append($"/image/{cameraShortName}?q={quality}&s={scale}");

        if (!string.IsNullOrWhiteSpace(Username) || !string.IsNullOrWhiteSpace(Password))
        {
            builder
                .Append("&user=")
                .Append(Uri.EscapeDataString(Username))
                .Append("&pw=")
                .Append(Uri.EscapeDataString(Password));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Fetches a snapshot image as bytes.
    /// </summary>
    /// <param name="cameraShortName">The Blue Iris camera short name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JPEG image bytes.</returns>
    public async Task<byte[]> GetSnapshotAsync(string cameraShortName, CancellationToken cancellationToken = default)
    {
        var url = BuildSnapshotUrl(cameraShortName);
        using var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["cmd"] = "login",
            ["username"] = Username,
            ["password"] = Password
        };

        using var response = await PostJsonAsync(payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.String
            && string.Equals(result.GetString(), "success", StringComparison.OrdinalIgnoreCase))
        {
            _session = GetString(document.RootElement, "session") ?? _session;
            return;
        }

        _session = null;
        throw new InvalidOperationException("Blue Iris login failed.");
    }

    private async Task<HttpResponseMessage> PostJsonAsync(Dictionary<string, object?> payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("Blue Iris BaseUrl is not configured.");
        }

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(BaseUrl), "json"));

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        return await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }
}
