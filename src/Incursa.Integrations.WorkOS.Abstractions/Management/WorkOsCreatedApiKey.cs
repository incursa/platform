namespace Incursa.Integrations.WorkOS.Abstractions.Management;

public sealed record WorkOsCreatedApiKey(
    string ApiKeyId,
    string DisplayName,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ExpiresUtc,
    IReadOnlyCollection<string> EffectiveScopes,
    string OrganizationId,
    string TenantId,
    string Secret);

