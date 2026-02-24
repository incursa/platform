using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Health;

public static class PlatformHealthChecksBuilderExtensions
{
    public static IHealthChecksBuilder AddLiveCheck<THealthCheck>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus = null)
        where THealthCheck : class, IHealthCheck
    {
        return builder.AddCheck<THealthCheck>(name, failureStatus, [PlatformHealthTags.Live]);
    }

    public static IHealthChecksBuilder AddReadyCheck<THealthCheck>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus = null)
        where THealthCheck : class, IHealthCheck
    {
        return builder.AddCheck<THealthCheck>(name, failureStatus, [PlatformHealthTags.Ready]);
    }

    public static IHealthChecksBuilder AddDependencyCheck<THealthCheck>(
        this IHealthChecksBuilder builder,
        string name,
        HealthStatus? failureStatus = null)
        where THealthCheck : class, IHealthCheck
    {
        return builder.AddCheck<THealthCheck>(name, failureStatus, [PlatformHealthTags.Dep]);
    }

    public static IHealthChecksBuilder AddLiveCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<HealthCheckResult> check,
        HealthStatus? failureStatus = null)
    {
        return AddDelegateCheck(builder, name, check, failureStatus, [PlatformHealthTags.Live]);
    }

    public static IHealthChecksBuilder AddReadyCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<HealthCheckResult> check,
        HealthStatus? failureStatus = null)
    {
        return AddDelegateCheck(builder, name, check, failureStatus, [PlatformHealthTags.Ready]);
    }

    public static IHealthChecksBuilder AddDependencyCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<HealthCheckResult> check,
        HealthStatus? failureStatus = null)
    {
        return AddDelegateCheck(builder, name, check, failureStatus, [PlatformHealthTags.Dep]);
    }

    public static IHealthChecksBuilder AddCheckInBuckets<THealthCheck>(
        this IHealthChecksBuilder builder,
        string name,
        PlatformHealthBucket buckets,
        HealthStatus? failureStatus = null)
        where THealthCheck : class, IHealthCheck
    {
        return builder.AddCheck<THealthCheck>(name, failureStatus, ToTags(buckets));
    }

    public static IHealthChecksBuilder AddCheckInBuckets(
        this IHealthChecksBuilder builder,
        string name,
        Func<HealthCheckResult> check,
        PlatformHealthBucket buckets,
        HealthStatus? failureStatus = null)
    {
        return AddDelegateCheck(builder, name, check, failureStatus, ToTags(buckets));
    }

    public static IHealthChecksBuilder AddCachedDependencyCheck<THealthCheck>(
        this IHealthChecksBuilder builder,
        string name,
        Action<CachedHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null)
        where THealthCheck : class, IHealthCheck
    {
        return builder.AddCachedCheck<THealthCheck>(
            name,
            options =>
            {
                options.HealthyCacheDuration = PlatformHealthConstants.DefaultDependencySuccessCacheTtl;
                options.DegradedCacheDuration = PlatformHealthConstants.DefaultDependencyFailureCacheTtl;
                options.UnhealthyCacheDuration = PlatformHealthConstants.DefaultDependencyFailureCacheTtl;
                configure?.Invoke(options);
            },
            failureStatus,
            [PlatformHealthTags.Dep]);
    }

    public static IHealthChecksBuilder AddCachedCheck<THealthCheck>(
        this IHealthChecksBuilder builder,
        string name,
        Action<CachedHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
        where THealthCheck : class, IHealthCheck
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        RegisterCachingOptions(builder, name, configure);

        var serviceKey = $"CachedHealthCheck_{name}";
        builder.Services.AddKeyedSingleton<IHealthCheck>(serviceKey, (sp, key) =>
        {
            var inner = ActivatorUtilities.GetServiceOrCreateInstance<THealthCheck>(sp);
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CachedHealthCheckOptions>>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new CachedHealthCheck(inner, optionsMonitor.Get(name), timeProvider);
        });

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredKeyedService<IHealthCheck>(serviceKey),
            failureStatus,
            tags));
    }

    public static IHealthChecksBuilder AddCachedCheck(
        this IHealthChecksBuilder builder,
        string name,
        Func<IServiceProvider, CancellationToken, Task<HealthCheckResult>> check,
        Action<CachedHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(check);

        RegisterCachingOptions(builder, name, configure);

        var serviceKey = $"CachedHealthCheck_{name}";
        builder.Services.AddKeyedSingleton<IHealthCheck>(serviceKey, (sp, key) =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CachedHealthCheckOptions>>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new CachedHealthCheck(new DelegateHealthCheck(ct => check(sp, ct)), optionsMonitor.Get(name), timeProvider);
        });

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredKeyedService<IHealthCheck>(serviceKey),
            failureStatus,
            tags));
    }

    private static string[] ToTags(PlatformHealthBucket buckets)
    {
        var tags = new List<string>(3);
        if (buckets.HasFlag(PlatformHealthBucket.Live))
        {
            tags.Add(PlatformHealthTags.Live);
        }

        if (buckets.HasFlag(PlatformHealthBucket.Ready))
        {
            tags.Add(PlatformHealthTags.Ready);
        }

        if (buckets.HasFlag(PlatformHealthBucket.Dep))
        {
            tags.Add(PlatformHealthTags.Dep);
        }

        if (tags.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buckets), buckets, "At least one health bucket is required.");
        }

        return [.. tags];
    }

    private static void RegisterCachingOptions(
        IHealthChecksBuilder builder,
        string name,
        Action<CachedHealthCheckOptions>? configure)
    {
        if (configure is not null)
        {
            builder.Services.Configure(name, configure);
        }
        else
        {
            builder.Services.Configure<CachedHealthCheckOptions>(name, _ => { });
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<CachedHealthCheckOptions>>(new CachedHealthCheckOptionsValidator()));
    }

    private static IHealthChecksBuilder AddDelegateCheck(
        IHealthChecksBuilder builder,
        string name,
        Func<HealthCheckResult> check,
        HealthStatus? failureStatus,
        IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(check);

        return builder.Add(new HealthCheckRegistration(
            name,
            _ => new DelegateHealthCheck(_ => Task.FromResult(check())),
            failureStatus,
            tags));
    }

    private sealed class DelegateHealthCheck : IHealthCheck
    {
        private readonly Func<CancellationToken, Task<HealthCheckResult>> check;

        public DelegateHealthCheck(Func<CancellationToken, Task<HealthCheckResult>> check)
        {
            this.check = check ?? throw new ArgumentNullException(nameof(check));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return check(cancellationToken);
        }
    }
}
