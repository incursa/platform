namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsClaimsEnrichmentOptions
{
    public string OrganizationIdClaimType { get; set; } = "org_id";

    public string RoleClaimType { get; set; } = "workos:role";

    public string PermissionClaimType { get; set; } = "workos:permission";

    public bool EnableApiFallback { get; set; } = true;
}
