using System.Net;
using System.Text.Json;
using Incursa.Platform.Health;
using Incursa.Platform.Health.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Tests;

public sealed class StandardizedHealthEndpointTests
{
    [Fact]
    public async Task DepEndpoint_Returns503AndStandardPayload_WhenDependencyFails()
    {
        await using var app = await BuildAppAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync(new Uri(PlatformHealthEndpoints.Dep, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        using var payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);

        payload.RootElement.GetProperty("bucket").GetString().ShouldBe(PlatformHealthTags.Dep);
        payload.RootElement.GetProperty("status").GetString().ShouldBe(HealthStatus.Unhealthy.ToString());
        payload.RootElement.GetProperty("totalDurationMs").ValueKind.ShouldBe(JsonValueKind.Number);
        var checks = payload.RootElement.GetProperty("checks");
        checks.ValueKind.ShouldBe(JsonValueKind.Array);
        checks.GetArrayLength().ShouldBeGreaterThan(0);
    }

    private static async Task<WebApplication> BuildAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddPlatformHealthChecks()
            .AddDependencyCheck("dependency_failure", static () => HealthCheckResult.Unhealthy("Dependency down"));

        var app = builder.Build();
        app.MapPlatformHealthEndpoints();

        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return app;
    }
}
