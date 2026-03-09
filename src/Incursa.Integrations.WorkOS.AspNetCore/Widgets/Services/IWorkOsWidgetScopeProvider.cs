namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;

public interface IWorkOsWidgetScopeProvider
{
    IReadOnlyList<string> GetScopes(WorkOsWidgetType widgetType);
}
