using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Incursa.Integrations.Cloudflare.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class CloudflareCustomHostnameClientTests
{
    [Fact]
    public async Task GetByHostnameAsync_UsesCustomHostnameZone_WhenConfiguredAsync()
    {
        string? path = null;
        StubHttpMessageHandler handler = new(async (request, _) =>
        {
            path = request.RequestUri?.PathAndQuery;
            return await Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true,\"result\":{\"result\":[{\"id\":\"h1\"}]}}"),
            });
        });

        var sut = CreateClient(handler, apiZoneId: "api-zone", customHostnameZoneId: "custom-zone");
        _ = await sut.GetByHostnameAsync("tenant.example.com", CancellationToken.None);

        Assert.NotNull(path);
        Assert.Contains("/zones/custom-zone/custom_hostnames", path);
    }

    [Fact]
    public async Task GetByHostnameAsync_ReturnsNull_WhenNoItemsAsync()
    {
        StubHttpMessageHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\":true,\"result\":{\"result\":[]}}"),
        }));

        var sut = CreateClient(handler);
        var result = await sut.GetByHostnameAsync("tenant.example.com", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task PatchAsync_Throws_WhenIdMissingAsync()
    {
        var sut = CreateClient(new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK))));

        await Assert.ThrowsAsync<ArgumentException>(() => sut.PatchAsync("   ", new Clients.Models.CloudflarePatchCustomHostnameRequest(), CancellationToken.None));
    }

    private static CloudflareCustomHostnameClient CreateClient(HttpMessageHandler handler, string apiZoneId = "zone-a", string? customHostnameZoneId = null)
    {
        HttpClient httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
        };

        var transport = new CloudflareApiTransport(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions
            {
                ApiToken = "token-123",
                ZoneId = apiZoneId,
            }),
            NullLogger<CloudflareApiTransport>.Instance);

        return new CloudflareCustomHostnameClient(
            transport,
            Microsoft.Extensions.Options.Options.Create(new CloudflareCustomHostnameOptions { ZoneId = customHostnameZoneId }),
            Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions { ZoneId = apiZoneId }));
    }
}
