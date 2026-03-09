namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;

internal sealed class MissingWorkOsWidgetIdentityResolver : IWorkOsWidgetIdentityResolver
{
    public Task<WorkOsWidgetIdentity> ResolveAsync(CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "No IWorkOsWidgetIdentityResolver is registered. Register one that resolves WorkOS organization/user identities for widget token issuance.");
    }
}
