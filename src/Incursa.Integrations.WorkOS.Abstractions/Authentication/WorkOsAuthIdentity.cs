namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public sealed record WorkOsAuthIdentity(
    string Subject,
    string ApiKeyId,
    string OrganizationId,
    string TenantId,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ExpiresUtc,
    DateTimeOffset? RevokedUtc,
    string DisplayName);

