namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Microsoft.Extensions.Options;

internal sealed class DefaultWorkOsWidgetScopeProvider : IWorkOsWidgetScopeProvider
{
    private static readonly IReadOnlyDictionary<WorkOsWidgetType, string[]> BuiltInScopes = new Dictionary<WorkOsWidgetType, string[]>
    {
        [WorkOsWidgetType.UsersManagement] = ["widgets:users-table:manage"],
        [WorkOsWidgetType.ApiKeys] = ["widgets:api-keys:manage"],
        [WorkOsWidgetType.DomainVerification] = ["widgets:domain-verification:manage"],
        [WorkOsWidgetType.SsoConnection] = ["widgets:sso:manage"],
    };

    private readonly IOptions<WorkOsWidgetsOptions> options;

    public DefaultWorkOsWidgetScopeProvider(IOptions<WorkOsWidgetsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options;
    }

    public IReadOnlyList<string> GetScopes(WorkOsWidgetType widgetType)
    {
        if (options.Value.WidgetScopes.TryGetValue(widgetType, out var configuredScopes))
        {
            return configuredScopes;
        }

        if (BuiltInScopes.TryGetValue(widgetType, out var defaultScopes))
        {
            return defaultScopes;
        }

        return [];
    }
}
