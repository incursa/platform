using System.Net;
using Incursa.Integrations.Cloudflare.Abstractions;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class CloudflareApiTransportTests
{
    [Fact]
    public async Task SendForResultAsync_ParsesSuccessEnvelopeAsync()
    {
        StubMessageHandler handler = new StubMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{" + "\"success\":true,\"result\":{\"value\":\"ok\"}}"),
            });

        var transport = CreateTransport(handler);
        var result = await transport.SendForResultAsync<TestPayload>(HttpMethod.Get, "zones/z1/custom_hostnames/h1", body: null, CancellationToken.None);

        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task SendForResultAsync_RetriesOnServerErrorAsync()
    {
        var attempt = 0;
        StubMessageHandler handler = new StubMessageHandler(_ =>
        {
            attempt++;
            if (attempt == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{" + "\"success\":false,\"errors\":[{\"code\":1001,\"message\":\"retry\"}]}"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{" + "\"success\":true,\"result\":{\"value\":\"ok\"}}"),
            };
        });

        var transport = CreateTransport(handler, retryCount: 2);
        var result = await transport.SendForResultAsync<TestPayload>(HttpMethod.Get, "zones/z1/load_balancers/lb1", body: null, CancellationToken.None);

        Assert.Equal("ok", result.Value);
        Assert.Equal(2, attempt);
    }

    [Fact]
    public async Task SendForResultAsync_ThrowsCloudflareApiException_OnCloudflareErrorAsync()
    {
        StubMessageHandler handler = new StubMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{" + "\"success\":false,\"errors\":[{\"code\":2000,\"message\":\"bad request\"}]}"),
            });

        var transport = CreateTransport(handler, retryCount: 0);
        var ex = await Assert.ThrowsAsync<CloudflareApiException>(() =>
            transport.SendForResultAsync<TestPayload>(HttpMethod.Post, "zones/z1/custom_hostnames", new { hostname = "a.example.com" }, CancellationToken.None));

        Assert.True(ex.Message.Contains("bad request", StringComparison.OrdinalIgnoreCase));
    }

    private static CloudflareApiTransport CreateTransport(HttpMessageHandler handler, int retryCount = 1)
    {
        HttpClient httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
        };

        var options = Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions
        {
            ApiToken = "token-123",
            RetryCount = retryCount,
            RequestTimeoutSeconds = 5,
        });

        return new CloudflareApiTransport(httpClient, options, NullLogger<CloudflareApiTransport>.Instance);
    }

    private sealed record TestPayload([property: JsonPropertyName("value")] string Value);

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public StubMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}
