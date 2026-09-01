using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.BlueIris.Api;
using Jellyfin.Plugin.BlueIris.BlueIris;
using Jellyfin.Plugin.BlueIris.Configuration;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.BlueIris.Tests;

/// <summary>
/// Unit tests for <see cref="BlueIrisController"/>.
/// </summary>
public class BlueIrisControllerTests
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

    private static BlueIrisClient CreateClient(byte[]? image = null, bool fail = false)
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            if (fail)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(image ?? new byte[] { 0xFF, 0xD8, 0xFF })
            });
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://bi/")
        };

        var client = new BlueIrisClient("http://bi/", httpClient)
        {
            Username = "user",
            Password = "pass"
        };

        return client;
    }

    private static void SetPluginConfiguration(PluginConfiguration configuration)
    {
#pragma warning disable SYSLIB0050
        var plugin = FormatterServices.GetUninitializedObject(typeof(Plugin));
#pragma warning restore SYSLIB0050

        var configurationProperty = typeof(Plugin).GetProperty("Configuration", BindingFlags.Public | BindingFlags.Instance);
        configurationProperty?.SetValue(plugin, configuration);

        var instanceProperty = typeof(Plugin).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        instanceProperty?.SetValue(null, plugin);
    }

    [Fact]
    public async Task GetSnapshot_Succeeds_ReturnsFileContentResult()
    {
        SetPluginConfiguration(new PluginConfiguration
        {
            ServerUrl = "http://bi/",
            Username = "user",
            Password = "pass"
        });

        var expected = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var client = CreateClient(expected);
        var controller = new BlueIrisController(client);

        var result = await controller.GetSnapshot("Cam1");

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal(expected, fileResult.FileContents);
    }

    [Fact]
    public async Task GetSnapshot_Fails_ReturnsNotFound()
    {
        SetPluginConfiguration(new PluginConfiguration
        {
            ServerUrl = "http://bi/",
            Username = "user",
            Password = "pass"
        });

        var client = CreateClient(fail: true);
        var controller = new BlueIrisController(client);

        var result = await controller.GetSnapshot("UnknownCam");

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
