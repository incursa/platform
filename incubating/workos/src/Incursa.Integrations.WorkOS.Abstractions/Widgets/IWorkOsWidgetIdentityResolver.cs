namespace Incursa.Integrations.WorkOS.Abstractions.Widgets;

public interface IWorkOsWidgetIdentityResolver
{
    Task<WorkOsWidgetIdentity> ResolveAsync(CancellationToken cancellationToken);
}
