namespace Incursa.Integrations.WorkOS.Core.Profiles;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Profiles;

public sealed class WorkOsUserProfileProvider : IWorkOsUserProfileProvider
{
    private readonly IWorkOsMembershipClient membershipClient;
    private readonly IWorkOsUserProfileCache cache;
    private readonly WorkOsUserProfileHydrationOptions options;

    public WorkOsUserProfileProvider(
        IWorkOsMembershipClient membershipClient,
        IWorkOsUserProfileCache cache,
        WorkOsUserProfileHydrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(membershipClient);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);

        this.membershipClient = membershipClient;
        this.cache = cache;
        this.options = options;
    }

    public async ValueTask<WorkOsUserProfile?> GetProfileAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (!options.Enabled)
        {
            return null;
        }

        string? subject = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        WorkOsUserProfileCacheEntry? cachedEntry = await cache.GetAsync(subject, ct).ConfigureAwait(false);
        if (cachedEntry is not null)
        {
            bool stale = DateTimeOffset.UtcNow - cachedEntry.Profile.HydratedUtc >= options.CacheTtl;
            if (!stale || !options.RevalidateOnRequestIfStale)
            {
                return cachedEntry.Profile;
            }
        }

        WorkOsUserProfile fresh = await FetchProfileAsync(subject.Trim(), ct).ConfigureAwait(false);
        await cache.SetAsync(subject.Trim(), fresh, ct).ConfigureAwait(false);
        return fresh;
    }

    private async ValueTask<WorkOsUserProfile> FetchProfileAsync(string subject, CancellationToken ct)
    {
        IReadOnlyCollection<WorkOsOrganizationMembershipInfo> memberships = await membershipClient.ListOrganizationMembershipsAsync(subject, ct).ConfigureAwait(false);
        Dictionary<string, IReadOnlyCollection<string>> rolesByOrganization = new(StringComparer.Ordinal);
        HashSet<string> organizationIds = new(StringComparer.Ordinal);
        HashSet<string> permissions = new(StringComparer.Ordinal);

        foreach (WorkOsOrganizationMembershipInfo membership in memberships)
        {
            if (string.IsNullOrWhiteSpace(membership.OrganizationId))
            {
                continue;
            }

            string organizationId = membership.OrganizationId.Trim();
            organizationIds.Add(organizationId);

            IReadOnlyCollection<string> roleSlugs = membership.RoleSlugs
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static x => x, StringComparer.Ordinal)
                .ToArray();

            rolesByOrganization[organizationId] = roleSlugs;

            IReadOnlyCollection<string> organizationPermissions = await membershipClient.ListRolePermissionsAsync(organizationId, roleSlugs, ct).ConfigureAwait(false);
            foreach (string permission in organizationPermissions)
            {
                if (!string.IsNullOrWhiteSpace(permission))
                {
                    permissions.Add(permission.Trim());
                }
            }
        }

        return new WorkOsUserProfile(
            Subject: subject,
            OrganizationIds: organizationIds.OrderBy(static x => x, StringComparer.Ordinal).ToArray(),
            RolesByOrganization: rolesByOrganization.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.Ordinal),
            Permissions: permissions.OrderBy(static x => x, StringComparer.Ordinal).ToArray(),
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal),
            HydratedUtc: DateTimeOffset.UtcNow);
    }
}
