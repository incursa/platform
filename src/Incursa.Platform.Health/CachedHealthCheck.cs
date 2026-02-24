using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Health;

public sealed class CachedHealthCheck : IHealthCheck, IDisposable
{
    private readonly IHealthCheck innerHealthCheck;
    private readonly CachedHealthCheckOptions options;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private HealthCheckResult? cachedResult;
    private DateTimeOffset lastCheckTime = DateTimeOffset.MinValue;

    public CachedHealthCheck(IHealthCheck innerHealthCheck, CachedHealthCheckOptions options, TimeProvider? timeProvider = null)
    {
        this.innerHealthCheck = innerHealthCheck ?? throw new ArgumentNullException(nameof(innerHealthCheck));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (ShouldUseCache(cachedResult, lastCheckTime, now))
        {
            return cachedResult!.Value;
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (ShouldUseCache(cachedResult, lastCheckTime, timeProvider.GetUtcNow()))
            {
                return cachedResult!.Value;
            }

            var result = await innerHealthCheck.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
            cachedResult = result;
            lastCheckTime = timeProvider.GetUtcNow();
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    private bool ShouldUseCache(HealthCheckResult? result, DateTimeOffset lastCheck, DateTimeOffset now)
    {
        if (result is null)
        {
            return false;
        }

        var elapsed = now - lastCheck;
        return elapsed < options.GetCacheDuration(result.Value.Status);
    }
}
