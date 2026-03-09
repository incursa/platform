namespace Incursa.Integrations.WorkOS.Core.Profiles;

using System.Security.Claims;
using System.Text.Json;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Profiles;

public sealed class WorkOsUserProfileProjector : IWorkOsUserProfileProjector
{
    private readonly WorkOsClaimsEnrichmentOptions claimsOptions;
    private readonly WorkOsUserProfileHydrationOptions hydrationOptions;

    public WorkOsUserProfileProjector(
        WorkOsClaimsEnrichmentOptions claimsOptions,
        WorkOsUserProfileHydrationOptions hydrationOptions)
    {
        ArgumentNullException.ThrowIfNull(claimsOptions);
        ArgumentNullException.ThrowIfNull(hydrationOptions);

        this.claimsOptions = claimsOptions;
        this.hydrationOptions = hydrationOptions;
    }

    public void ProjectToClaims(WorkOsUserProfile profile, ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(identity);

        string organizationClaimType = claimsOptions.OrganizationIdClaimType;
        string roleClaimType = claimsOptions.RoleClaimType;
        string permissionClaimType = claimsOptions.PermissionClaimType;

        IReadOnlyCollection<Claim> existing = identity.Claims.ToArray();
        foreach (Claim claim in existing)
        {
            if (string.Equals(claim.Type, organizationClaimType, StringComparison.Ordinal)
                || string.Equals(claim.Type, roleClaimType, StringComparison.Ordinal)
                || string.Equals(claim.Type, permissionClaimType, StringComparison.Ordinal)
                || claim.Type.StartsWith(hydrationOptions.ClaimPrefix, StringComparison.Ordinal))
            {
                identity.RemoveClaim(claim);
            }
        }

        foreach (string organizationId in profile.OrganizationIds)
        {
            identity.AddClaim(new Claim(organizationClaimType, organizationId));
        }

        HashSet<string> roleSet = new(StringComparer.Ordinal);
        foreach (IReadOnlyCollection<string> roles in profile.RolesByOrganization.Values)
        {
            foreach (string role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    roleSet.Add(role.Trim());
                }
            }
        }

        foreach (string role in roleSet.OrderBy(static x => x, StringComparer.Ordinal))
        {
            identity.AddClaim(new Claim(roleClaimType, role));
        }

        foreach (string permission in profile.Permissions)
        {
            identity.AddClaim(new Claim(permissionClaimType, permission));
        }

        foreach ((string key, string value) in profile.Metadata)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                identity.AddClaim(new Claim(hydrationOptions.ClaimPrefix + key.Trim(), value.Trim()));
            }
        }

        if (hydrationOptions.IncludeRawProfileJson)
        {
            string rawJson = JsonSerializer.Serialize(profile);
            identity.AddClaim(new Claim(hydrationOptions.ClaimPrefix + "raw_json", rawJson));
        }
    }
}
