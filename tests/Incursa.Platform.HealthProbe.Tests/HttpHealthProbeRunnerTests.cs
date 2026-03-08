using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.HealthProbe.Tests;

public sealed class HttpHealthProbeRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsNonHealthy_WhenPayloadIsInvalid()
    {
        using var handler = new StubHttpMessageHandler(static _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "application/json"),
            });
        var runner = CreateRunner(handler);

        var result = await runner.RunAsync(
            new HealthProbeRequest("ready", false),
            TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(HealthProbeExitCodes.NonHealthy);
        result.Status.ShouldBe("Unhealthy");
    }

    [Fact]
    public async Task RunAsync_ReturnsNonHealthy_WhenTransportFails()
    {
        using var handler = new StubHttpMessageHandler(static _ => throw new HttpRequestException("connection refused"));
        var runner = CreateRunner(handler);

        var result = await runner.RunAsync(
            new HealthProbeRequest("ready", false),
            TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(HealthProbeExitCodes.NonHealthy);
        result.Status.ShouldBe("Unhealthy");
    }

    [Fact]
    public async Task RunAsync_Throws_WhenBaseUrlIsMissingForRelativePath()
    {
        using var handler = new StubHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.OK));
        var runner = CreateRunner(
            handler,
            configure: options =>
            {
                options.Http.BaseUrl = null;
            });

        await Should.ThrowAsync<HealthProbeArgumentException>(() =>
            runner.RunAsync(new HealthProbeRequest("ready", false), TestContext.Current.CancellationToken));
    }

    private static HttpHealthProbeRunner CreateRunner(
        HttpMessageHandler messageHandler,
        Action<HealthProbeOptions>? configure = null)
    {
        var options = new HealthProbeOptions
        {
            Mode = HealthProbeMode.Http,
            Http = new HealthProbeHttpOptions
            {
                BaseUrl = new Uri("https://probe.example.local"),
            },
        };
        configure?.Invoke(options);

        var client = new HttpClient(messageHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        var factory = new StubHttpClientFactory(client);
        return new HttpHealthProbeRunner(factory, NullLogger<HttpHealthProbeRunner>.Instance, Options.Create(options));
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient httpClient;

        public StubHttpClientFactory(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public HttpClient CreateClient(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return httpClient;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
