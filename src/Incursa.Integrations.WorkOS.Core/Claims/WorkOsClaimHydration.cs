namespace Incursa.Integrations.WorkOS.Core.Claims;

using System.Net.Http.Headers;
using Incursa.Integrations.WorkOS.Abstractions.Claims;

public static class WorkOsClaimHydration
{
    public static async ValueTask<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>> ListOrganizationMembershipsAsync(
        HttpClient httpClient,
        string apiKey,
        string userId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<WorkOsOrganizationMembershipInfo>();
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var path = $"user_management/organization_memberships?user_id={Uri.EscapeDataString(userId)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<WorkOsOrganizationMembershipInfo>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<WorkOsOrganizationMembershipInfo>();
        }

        List<WorkOsOrganizationMembershipInfo> result = [];
        foreach (var item in data.EnumerateArray())
        {
            var orgId = TryGetString(item, "organization_id") ?? TryGetString(item, "organizationId");
            if (string.IsNullOrWhiteSpace(orgId))
            {
                continue;
            }

            var roles = EnumerateRoleSlugs(item)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            result.Add(new WorkOsOrganizationMembershipInfo(orgId.Trim(), roles));
        }

        return result;
    }

    public static async ValueTask<IReadOnlyCollection<string>> ListRolePermissionsAsync(
        HttpClient httpClient,
        string apiKey,
        string organizationId,
        IEnumerable<string> roleSlugs,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(roleSlugs);

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(organizationId))
        {
            return Array.Empty<string>();
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        HashSet<string> permissions = new(StringComparer.Ordinal);

        foreach (var roleSlug in roleSlugs.Where(static x => !string.IsNullOrWhiteSpace(x)).Select(static x => x.Trim()).Distinct(StringComparer.Ordinal))
        {
            var path = $"authorization/organizations/{Uri.EscapeDataString(organizationId)}/roles/{Uri.EscapeDataString(roleSlug)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("permissions", out var perms) || perms.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var permission in perms.EnumerateArray())
            {
                if (permission.ValueKind == JsonValueKind.String)
                {
                    var value = permission.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        permissions.Add(value.Trim());
                    }
                }
            }
        }

        return permissions.OrderBy(static x => x, StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> EnumerateRoleSlugs(JsonElement membership)
    {
        if (membership.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
        {
            foreach (var role in roles.EnumerateArray())
            {
                var slug = TryGetString(role, "slug");
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    yield return slug;
                }
            }
        }

        if (membership.TryGetProperty("role", out var roleObject) && roleObject.ValueKind == JsonValueKind.Object)
        {
            var slug = TryGetString(roleObject, "slug");
            if (!string.IsNullOrWhiteSpace(slug))
            {
                yield return slug;
            }
        }
    }

    private static string? TryGetString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
