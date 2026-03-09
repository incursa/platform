namespace Incursa.Integrations.WorkOS.Core.Claims;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Profiles;
using Microsoft.Extensions.DependencyInjection;

public sealed class WorkOsClaimsEnricher : IWorkOsClaimsEnricher
{
    private readonly IWorkOsMembershipClient? legacyMembershipClient;
    private readonly IWorkOsUserProfileProvider? profileProvider;
    private readonly IWorkOsUserProfileProjector? profileProjector;
    private readonly WorkOsClaimsEnrichmentOptions options;

    public WorkOsClaimsEnricher(
        IWorkOsMembershipClient membershipClient,
        WorkOsClaimsEnrichmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(membershipClient);
        ArgumentNullException.ThrowIfNull(options);

        legacyMembershipClient = membershipClient;
        this.options = options;
    }

    [ActivatorUtilitiesConstructor]
    public WorkOsClaimsEnricher(
        IWorkOsUserProfileProvider profileProvider,
        IWorkOsUserProfileProjector profileProjector,
        WorkOsClaimsEnrichmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(profileProvider);
        ArgumentNullException.ThrowIfNull(profileProjector);
        ArgumentNullException.ThrowIfNull(options);

        this.profileProvider = profileProvider;
        this.profileProjector = profileProjector;
        this.options = options;
    }

    public async ValueTask EnrichAsync(ClaimsPrincipal principal, ClaimsIdentity identity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(identity);

        if (!options.EnableApiFallback)
        {
            return;
        }

        if (profileProvider is not null && profileProjector is not null)
        {
            WorkOsUserProfile? profile = await profileProvider.GetProfileAsync(principal, ct).ConfigureAwait(false);
            if (profile is not null)
            {
                profileProjector.ProjectToClaims(profile, identity);
            }

            return;
        }

        if (legacyMembershipClient is null)
        {
            return;
        }

        string? userId = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        string organizationClaimType = options.OrganizationIdClaimType;
        string roleClaimType = options.RoleClaimType;
        string permissionClaimType = options.PermissionClaimType;

        bool hasOrganizations = principal.HasClaim(c => string.Equals(c.Type, organizationClaimType, StringComparison.Ordinal));
        bool hasRoles = principal.HasClaim(c => string.Equals(c.Type, roleClaimType, StringComparison.Ordinal));
        bool hasPermissions = principal.HasClaim(c => string.Equals(c.Type, permissionClaimType, StringComparison.Ordinal));

        if (hasOrganizations && hasRoles && hasPermissions)
        {
            return;
        }

        IReadOnlyCollection<WorkOsOrganizationMembershipInfo> memberships = await legacyMembershipClient.ListOrganizationMembershipsAsync(userId, ct).ConfigureAwait(false);
        foreach (WorkOsOrganizationMembershipInfo membership in memberships)
        {
            if (!identity.HasClaim(organizationClaimType, membership.OrganizationId))
            {
                identity.AddClaim(new Claim(organizationClaimType, membership.OrganizationId));
            }

            foreach (string role in membership.RoleSlugs)
            {
                if (!identity.HasClaim(roleClaimType, role))
                {
                    identity.AddClaim(new Claim(roleClaimType, role));
                }
            }
        }

        string? organizationId = principal.FindFirst(organizationClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return;
        }

        IEnumerable<string> roleSlugs = principal.FindAll(roleClaimType).Select(static c => c.Value).Where(static x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal);
        IReadOnlyCollection<string> permissions = await legacyMembershipClient.ListRolePermissionsAsync(organizationId, roleSlugs, ct).ConfigureAwait(false);
        foreach (string permission in permissions)
        {
            if (!identity.HasClaim(permissionClaimType, permission))
            {
                identity.AddClaim(new Claim(permissionClaimType, permission));
            }
        }
    }
}
