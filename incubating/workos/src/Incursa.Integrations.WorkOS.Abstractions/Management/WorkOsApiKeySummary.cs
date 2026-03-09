namespace Incursa.Integrations.WorkOS.Abstractions.Management;

public sealed record WorkOsApiKeySummary(
    string ApiKeyId,
    string DisplayName,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ExpiresUtc,
    DateTimeOffset? RevokedUtc,
    IReadOnlyCollection<string> EffectiveScopes,
    string OrganizationId,
    string TenantId);

