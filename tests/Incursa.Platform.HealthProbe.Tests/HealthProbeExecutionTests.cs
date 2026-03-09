using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Incursa.Platform.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Incursa.Platform.HealthProbe.Tests;

[Trait("Category", "Unit")]
[Trait("Category", "Smoke")]
public sealed class HealthProbeExecutionTests
{
    [Fact]
    public async Task Ready_WhenLatchHeld_ReturnsNonHealthyExitCode()
    {
        using var services = BuildServiceProvider();
        var latch = services.GetRequiredService<IStartupLatch>();
        using var step = latch.Register("bootstrap");

        var exitCode = await HealthProbeApp.RunHealthCheckAsync(
            ["health", "ready"],
            services,
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(HealthProbeExitCodes.NonHealthy);
    }

    [Fact]
    public async Task Live_WhenLatchHeld_ReturnsHealthyExitCode()
    {
        using var services = BuildServiceProvider();
        var latch = services.GetRequiredService<IStartupLatch>();
        using var step = latch.Register("bootstrap");

        var exitCode = await HealthProbeApp.RunHealthCheckAsync(
            ["health", "live"],
            services,
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(HealthProbeExitCodes.Healthy);
    }

    [Fact]
    public async Task Dep_WhenUnhealthyDependencyCheckExists_ReturnsNonHealthyExitCode()
    {
        var services = new ServiceCollection()
            .AddLogging();
        services.AddPlatformHealthChecks();
        services.AddIncursaHealthProbe();
        services.AddHealthChecks()
            .AddDependencyCheck("dep_test", static () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("failed"));

        using var provider = services.BuildServiceProvider();
        var exitCode = await HealthProbeApp.RunHealthCheckAsync(
            ["health", "dep"],
            provider,
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(HealthProbeExitCodes.NonHealthy);
    }

    [Fact]
    public async Task RunHealthCheckAsync_UsesConfiguredHttpMode_WhenModeIsHttp()
    {
        using var handler = new StubHttpMessageHandler(static request =>
        {
            var json = """
{
  "bucket": "ready",
  "status": "Healthy",
  "totalDurationMs": 1.5,
  "checks": []
}
""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        });

        using var services = BuildServiceProvider(
            configure: options =>
            {
                options.Mode = HealthProbeMode.Http;
                options.Http.BaseUrl = new Uri("https://probe.example.local");
                options.Http.ApiKey = "secret";
            },
            httpHandler: handler);

        var exitCode = await HealthProbeApp.RunHealthCheckAsync(
            ["health", "ready"],
            services,
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(HealthProbeExitCodes.Healthy);
        handler.RequestCount.ShouldBe(1);
        handler.LastRequest.ShouldNotBeNull();
        var requestUri = handler.LastRequest!.RequestUri;
        requestUri.ShouldNotBeNull();
        requestUri.IsAbsoluteUri.ShouldBeTrue();
        requestUri.AbsolutePath.ShouldBe("/readyz");

        if (!requestUri.IsFile)
        {
            requestUri.Scheme.ShouldBe(Uri.UriSchemeHttps);
            requestUri.Host.ShouldBe("probe.example.local");
        }

        handler.LastRequest.Headers.TryGetValues("X-Api-Key", out var apiKeyValues).ShouldBeTrue();
        apiKeyValues.ShouldNotBeNull();
        apiKeyValues.Single().ShouldBe("secret");
    }

    [Fact]
    public async Task RunHealthCheckAsync_ModeFlagOverridesConfiguration()
    {
        using var handler = new StubHttpMessageHandler(static _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var services = BuildServiceProvider(
            configure: options =>
            {
                options.Mode = HealthProbeMode.Http;
                options.Http.BaseUrl = new Uri("https://probe.example.local");
            },
            httpHandler: handler);

        var exitCode = await HealthProbeApp.RunHealthCheckAsync(
            ["health", "ready", "--mode", "inprocess"],
            services,
            TestContext.Current.CancellationToken);

        exitCode.ShouldBe(HealthProbeExitCodes.Healthy);
        handler.RequestCount.ShouldBe(0);
    }

    private static ServiceProvider BuildServiceProvider(
        Action<HealthProbeOptions>? configure = null,
        StubHttpMessageHandler? httpHandler = null)
    {
        var services = new ServiceCollection()
            .AddLogging();
        services.AddPlatformHealthChecks();
        services.AddIncursaHealthProbe(configure);

        if (httpHandler is not null)
        {
            services.AddSingleton<IHttpClientFactory>(_ =>
                new StubHttpClientFactory(new HttpClient(httpHandler)
                {
                    Timeout = Timeout.InfiniteTimeSpan,
                }));
        }

        return services.BuildServiceProvider();
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

        public HttpRequestMessage? LastRequest { get; private set; }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            RequestCount++;
            return Task.FromResult(responseFactory(request));
        }
    }
}
