namespace Incursa.Integrations.WorkOS.Core.Emulation;

using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Core.Clients;

public sealed class InMemoryWorkOsState : IInMemoryWorkOsState
{
    private readonly Dictionary<string, string> keyByOrgAndApiKeyId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WorkOsManagedApiKey> apiKeysBySecret = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<WorkOsOrganizationMembershipInfo>> membershipsByUser = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> adminsByOrganization = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> permissionsByOrganizationAndRole = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> tenantByOrganization = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> organizationByTenant = new(StringComparer.Ordinal);
    private readonly Lock gate = new();
    private long keySequence;

    public void Clear()
    {
        lock (gate)
        {
            keyByOrgAndApiKeyId.Clear();
            apiKeysBySecret.Clear();
            membershipsByUser.Clear();
            adminsByOrganization.Clear();
            permissionsByOrganizationAndRole.Clear();
            tenantByOrganization.Clear();
            organizationByTenant.Clear();
            keySequence = 0;
        }
    }

    public void SeedTenantMapping(string organizationId, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        lock (gate)
        {
            tenantByOrganization[organizationId.Trim()] = tenantId.Trim();
            organizationByTenant[tenantId.Trim()] = organizationId.Trim();
        }
    }

    public void SeedApiKey(string secret, WorkOsManagedApiKey apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentNullException.ThrowIfNull(apiKey);

        lock (gate)
        {
            string normalizedSecret = secret.Trim();
            apiKeysBySecret[normalizedSecret] = apiKey;
            keyByOrgAndApiKeyId[BuildOrgApiKey(apiKey.OrganizationId, apiKey.ApiKeyId)] = normalizedSecret;
        }
    }

    public void SeedOrganizationAdmin(string organizationId, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        lock (gate)
        {
            if (!adminsByOrganization.TryGetValue(organizationId.Trim(), out HashSet<string>? admins))
            {
                admins = new HashSet<string>(StringComparer.Ordinal);
                adminsByOrganization[organizationId.Trim()] = admins;
            }

            admins.Add(subject.Trim());
        }
    }

    public void SeedMembership(string userId, WorkOsOrganizationMembershipInfo membership)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(membership);

        lock (gate)
        {
            if (!membershipsByUser.TryGetValue(userId.Trim(), out List<WorkOsOrganizationMembershipInfo>? memberships))
            {
                memberships = [];
                membershipsByUser[userId.Trim()] = memberships;
            }

            memberships.Add(new WorkOsOrganizationMembershipInfo(
                membership.OrganizationId,
                membership.RoleSlugs.Where(static x => !string.IsNullOrWhiteSpace(x)).Select(static x => x.Trim()).Distinct(StringComparer.Ordinal).ToArray()));
        }
    }

    public void SeedRolePermissions(string organizationId, string roleSlug, IEnumerable<string> permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleSlug);
        ArgumentNullException.ThrowIfNull(permissions);

        lock (gate)
        {
            permissionsByOrganizationAndRole[BuildOrgRoleKey(organizationId.Trim(), roleSlug.Trim())] = permissions
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .ToHashSet(StringComparer.Ordinal);
        }
    }

    public WorkOsManagedApiKey? ValidateApiKey(string presentedKey)
    {
        if (string.IsNullOrWhiteSpace(presentedKey))
        {
            return null;
        }

        lock (gate)
        {
            return apiKeysBySecret.TryGetValue(presentedKey.Trim(), out WorkOsManagedApiKey? value) ? value : null;
        }
    }

    public WorkOsCreatedApiKey CreateApiKey(string organizationId, string displayName, IReadOnlyCollection<string> scopes, int? ttlHours)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(scopes);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset? expiresUtc = ttlHours.HasValue && ttlHours.Value > 0 ? now.AddHours(ttlHours.Value) : null;
        IReadOnlyCollection<string> normalizedScopes = scopes.Where(static x => !string.IsNullOrWhiteSpace(x)).Select(static x => x.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).OrderBy(static x => x, StringComparer.Ordinal).ToArray();

        lock (gate)
        {
            keySequence++;
            string sequence = keySequence.ToString(CultureInfo.InvariantCulture);
            string apiKeyId = "ikey_" + sequence.PadLeft(8, '0');
            string secret = "wos_test_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            string tenantId = tenantByOrganization.TryGetValue(organizationId.Trim(), out string? mappedTenant) ? mappedTenant : string.Empty;

            WorkOsManagedApiKey managed = new(
                apiKeyId,
                organizationId.Trim(),
                displayName.Trim(),
                now,
                expiresUtc,
                null,
                normalizedScopes);

            apiKeysBySecret[secret] = managed;
            keyByOrgAndApiKeyId[BuildOrgApiKey(organizationId.Trim(), apiKeyId)] = secret;

            return new WorkOsCreatedApiKey(apiKeyId, displayName.Trim(), now, expiresUtc, normalizedScopes, organizationId.Trim(), tenantId, secret);
        }
    }

    public IReadOnlyCollection<WorkOsApiKeySummary> ListApiKeys(string organizationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        lock (gate)
        {
            string tenantId = tenantByOrganization.TryGetValue(organizationId.Trim(), out string? mappedTenant) ? mappedTenant : string.Empty;
            return apiKeysBySecret.Values
                .Where(x => string.Equals(x.OrganizationId, organizationId.Trim(), StringComparison.Ordinal))
                .OrderByDescending(static x => x.CreatedUtc)
                .Select(x => new WorkOsApiKeySummary(x.ApiKeyId, x.DisplayName, x.CreatedUtc, x.ExpiresUtc, x.RevokedUtc, x.Permissions.Select(static p => p.ToLowerInvariant()).Distinct(StringComparer.Ordinal).OrderBy(static p => p, StringComparer.Ordinal).ToArray(), organizationId.Trim(), tenantId))
                .ToArray();
        }
    }

    public WorkOsApiKeySummary? GetApiKey(string organizationId, string apiKeyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyId);

        lock (gate)
        {
            string key = BuildOrgApiKey(organizationId.Trim(), apiKeyId.Trim());
            if (!keyByOrgAndApiKeyId.TryGetValue(key, out string? secret))
            {
                return null;
            }

            if (!apiKeysBySecret.TryGetValue(secret, out WorkOsManagedApiKey? managed))
            {
                return null;
            }

            string tenantId = tenantByOrganization.TryGetValue(organizationId.Trim(), out string? mappedTenant) ? mappedTenant : string.Empty;
            return new WorkOsApiKeySummary(
                managed.ApiKeyId,
                managed.DisplayName,
                managed.CreatedUtc,
                managed.ExpiresUtc,
                managed.RevokedUtc,
                managed.Permissions.Select(static p => p.ToLowerInvariant()).Distinct(StringComparer.Ordinal).OrderBy(static p => p, StringComparer.Ordinal).ToArray(),
                managed.OrganizationId,
                tenantId);
        }
    }

    public bool RevokeApiKey(string organizationId, string apiKeyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyId);

        lock (gate)
        {
            string key = BuildOrgApiKey(organizationId.Trim(), apiKeyId.Trim());
            if (!keyByOrgAndApiKeyId.TryGetValue(key, out string? secret))
            {
                return false;
            }

            if (!apiKeysBySecret.TryGetValue(secret, out WorkOsManagedApiKey? managed))
            {
                return false;
            }

            apiKeysBySecret[secret] = managed with { RevokedUtc = DateTimeOffset.UtcNow };
            return true;
        }
    }

    public bool IsOrganizationAdmin(string organizationId, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        lock (gate)
        {
            return adminsByOrganization.TryGetValue(organizationId.Trim(), out HashSet<string>? admins)
                && admins.Contains(subject.Trim());
        }
    }

    public IReadOnlyCollection<WorkOsOrganizationMembershipInfo> ListMemberships(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        lock (gate)
        {
            if (!membershipsByUser.TryGetValue(userId.Trim(), out List<WorkOsOrganizationMembershipInfo>? memberships))
            {
                return Array.Empty<WorkOsOrganizationMembershipInfo>();
            }

            return memberships
                .Select(static x => new WorkOsOrganizationMembershipInfo(x.OrganizationId, x.RoleSlugs))
                .ToArray();
        }
    }

    public IReadOnlyCollection<string> ListRolePermissions(string organizationId, IEnumerable<string> roleSlugs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentNullException.ThrowIfNull(roleSlugs);

        lock (gate)
        {
            HashSet<string> permissions = new(StringComparer.Ordinal);
            foreach (string roleSlug in roleSlugs.Where(static x => !string.IsNullOrWhiteSpace(x)).Select(static x => x.Trim()).Distinct(StringComparer.Ordinal))
            {
                if (permissionsByOrganizationAndRole.TryGetValue(BuildOrgRoleKey(organizationId.Trim(), roleSlug), out HashSet<string>? rolePermissions))
                {
                    permissions.UnionWith(rolePermissions);
                }
            }

            return permissions.OrderBy(static x => x, StringComparer.Ordinal).ToArray();
        }
    }

    public string? GetTenantId(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return null;
        }

        lock (gate)
        {
            return tenantByOrganization.TryGetValue(organizationId.Trim(), out string? tenantId) ? tenantId : null;
        }
    }

    public string? GetOrganizationId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        lock (gate)
        {
            return organizationByTenant.TryGetValue(tenantId.Trim(), out string? organizationId) ? organizationId : null;
        }
    }

    private static string BuildOrgApiKey(string organizationId, string apiKeyId)
        => organizationId + "|" + apiKeyId;

    private static string BuildOrgRoleKey(string organizationId, string roleSlug)
        => organizationId + "|" + roleSlug;
}
