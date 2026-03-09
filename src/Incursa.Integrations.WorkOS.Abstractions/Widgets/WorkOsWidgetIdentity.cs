namespace Incursa.Integrations.WorkOS.Abstractions.Widgets;

public sealed record WorkOsWidgetIdentity(
    string OrganizationId,
    string UserId,
    string? OrganizationExternalId = null);
