namespace Incursa.Integrations.WorkOS.Abstractions.Audit;

public sealed record WorkOsAuditContext(
    string? Location = null,
    string? UserAgent = null);
