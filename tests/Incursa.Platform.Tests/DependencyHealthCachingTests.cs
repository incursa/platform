using Incursa.Platform.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Time.Testing;

namespace Incursa.Platform.Tests;

public sealed class DependencyHealthCachingTests
{
    [Fact]
    public async Task DependencyCheck_UsesCacheWindow_ByDefault()
    {
        var fakeTime = new FakeTimeProvider(DateTimeOffset.Parse("2024-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(fakeTime);
        services.AddSingleton<CountingDependencyHealthCheck>();
        services
            .AddHealthChecks()
            .AddCachedDependencyCheck<CountingDependencyHealthCheck>("dep_test");

        using var provider = services.BuildServiceProvider();
        var health = provider.GetRequiredService<HealthCheckService>();
        var inner = provider.GetRequiredService<CountingDependencyHealthCheck>();

        await health.CheckHealthAsync(reg => reg.Tags.Contains(PlatformHealthTags.Dep, StringComparer.Ordinal), TestContext.Current.CancellationToken);
        fakeTime.Advance(TimeSpan.FromSeconds(30));
        await health.CheckHealthAsync(reg => reg.Tags.Contains(PlatformHealthTags.Dep, StringComparer.Ordinal), TestContext.Current.CancellationToken);

        inner.InvocationCount.ShouldBe(1);
    }

    private sealed class CountingDependencyHealthCheck : IHealthCheck
    {
        public int InvocationCount { get; private set; }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(HealthCheckResult.Healthy("ok"));
        }
    }
}
