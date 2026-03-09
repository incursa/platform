namespace Incursa.Integrations.WorkOS.Abstractions.Profiles;

public sealed record WorkOsUserProfileCacheEntry(WorkOsUserProfile Profile, DateTimeOffset CachedUtc);

public interface IWorkOsUserProfileCache
{
    ValueTask<WorkOsUserProfileCacheEntry?> GetAsync(string subject, CancellationToken ct = default);

    ValueTask SetAsync(string subject, WorkOsUserProfile profile, CancellationToken ct = default);

    ValueTask RemoveAsync(string subject, CancellationToken ct = default);
}
