using Incursa.Platform.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Incursa.Platform.HealthProbe.Tests;

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

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection()
            .AddLogging();
        services.AddPlatformHealthChecks();
        services.AddIncursaHealthProbe();

        return services.BuildServiceProvider();
    }
}
