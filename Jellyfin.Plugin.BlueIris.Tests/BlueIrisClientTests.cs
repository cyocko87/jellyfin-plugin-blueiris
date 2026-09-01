using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.BlueIris.BlueIris;
using Xunit;

namespace Jellyfin.Plugin.BlueIris.Tests;

/// <summary>
/// Unit tests for <see cref="BlueIrisClient"/>.
/// </summary>
public class BlueIrisClientTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _sendAsync(request, cancellationToken);
        }
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new HttpClient(new FakeHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("http://bi/")
        };
    }

    [Fact]
    public async Task GetCamerasAsync_ParsesCamList()
    {
        var response = new
        {
            result = "success",
            data = new[]
            {
                new { optionValue = "Cam1", optionDisplay = "Front Porch", group = "Default" },
                new { optionValue = "Cam2", optionDisplay = "Back Yard", group = "Default" }
            }
        };

        var json = JsonSerializer.Serialize(response);

        var client = CreateClient((request, _) =>
        {
            Assert.Equal("http://bi/json", request.RequestUri?.ToString());
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        });

        var blueIris = new BlueIrisClient("http://bi/", client);
        var cameras = await blueIris.GetCamerasAsync().ConfigureAwait(false);

        Assert.Equal(2, cameras.Count);
        Assert.Equal("Cam1", cameras[0].ShortName);
        Assert.Equal("Front Porch", cameras[0].DisplayName);
        Assert.Equal("Cam2", cameras[1].ShortName);
        Assert.Equal("Back Yard", cameras[1].DisplayName);
    }

    [Fact]
    public async Task GetCamerasAsync_ReturnsEmptyWhenDataMissing()
    {
        var response = new { result = "success" };
        var json = JsonSerializer.Serialize(response);

        var client = CreateClient((_, _) =>
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        });

        var blueIris = new BlueIrisClient("http://bi/", client);
        var cameras = await blueIris.GetCamerasAsync().ConfigureAwait(false);

        Assert.Empty(cameras);
    }

    [Fact]
    public void BuildStreamUrl_Hls_WithAuth()
    {
        var client = new BlueIrisClient("http://192.168.5.5:81")
        {
            Username = "user",
            Password = "p w"
        };

        var url = client.BuildStreamUrl("Cam1", "HLS");

        Assert.Equal("http://192.168.5.5:81/h264/Cam1/temp.m3u8?user=user&pw=p%20w", url);
    }

    [Fact]
    public void BuildStreamUrl_Mjpeg()
    {
        var client = new BlueIrisClient("http://192.168.5.5:81")
        {
            Username = "user",
            Password = "pass"
        };

        var url = client.BuildStreamUrl("Cam2", "MJPEG");

        Assert.Equal("http://192.168.5.5:81/mjpg/Cam2/video.mjpg?user=user&pw=pass", url);
    }

    [Fact]
    public void BuildSnapshotUrl_NoAuth()
    {
        var client = new BlueIrisClient("http://192.168.5.5:81");

        var url = client.BuildSnapshotUrl("Cam1");

        Assert.Equal("http://192.168.5.5:81/image/Cam1?q=50&s=80", url);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReturnsImageBytes()
    {
        var expected = new byte[] { 0xFF, 0xD8, 0xFF };

        var client = CreateClient((request, _) =>
        {
            Assert.Contains("/image/Cam1", request.RequestUri?.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expected)
            });
        });

        var blueIris = new BlueIrisClient("http://192.168.5.5:81", client);
        var image = await blueIris.GetSnapshotAsync("Cam1").ConfigureAwait(false);

        Assert.Equal(expected, image);
    }
}
