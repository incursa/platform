namespace Incursa.Integrations.WorkOS.Core.Profiles;

using Incursa.Integrations.WorkOS.Abstractions.Profiles;
using Microsoft.Extensions.Caching.Memory;

public sealed class MemoryWorkOsUserProfileCache : IWorkOsUserProfileCache
{
    private readonly IMemoryCache memoryCache;

    public MemoryWorkOsUserProfileCache(IMemoryCache memoryCache)
    {
        ArgumentNullException.ThrowIfNull(memoryCache);
        this.memoryCache = memoryCache;
    }

    public ValueTask<WorkOsUserProfileCacheEntry?> GetAsync(string subject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return ValueTask.FromResult<WorkOsUserProfileCacheEntry?>(null);
        }

        return ValueTask.FromResult(memoryCache.TryGetValue(GetCacheKey(subject), out WorkOsUserProfileCacheEntry? entry) ? entry : null);
    }

    public ValueTask SetAsync(string subject, WorkOsUserProfile profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentNullException.ThrowIfNull(profile);

        memoryCache.Set(GetCacheKey(subject), new WorkOsUserProfileCacheEntry(profile, DateTimeOffset.UtcNow));
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string subject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(subject))
        {
            memoryCache.Remove(GetCacheKey(subject));
        }

        return ValueTask.CompletedTask;
    }

    private static string GetCacheKey(string subject)
        => "workos:user_profile:" + subject.Trim();
}
