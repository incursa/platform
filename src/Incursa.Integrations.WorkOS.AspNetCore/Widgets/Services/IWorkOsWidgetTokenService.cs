namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;

public interface IWorkOsWidgetTokenService
{
    Task<string> CreateTokenAsync(WorkOsWidgetType widgetType, WorkOsWidgetIdentity identity, CancellationToken cancellationToken);
}
