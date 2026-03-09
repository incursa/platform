namespace Incursa.Integrations.WorkOS.AspNetCore.DependencyInjection;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Infrastructure;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public static class WorkOsWidgetsServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsWidgets(
        this IServiceCollection services,
        Action<WorkOsWidgetsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddHttpContextAccessor();
        services.AddOptions<WorkOsWidgetsOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WorkOsWidgetsOptions>, WorkOsWidgetsOptionsValidation>());
        services.TryAddSingleton<IWorkOsWidgetScopeProvider, DefaultWorkOsWidgetScopeProvider>();
        services.TryAddScoped<IWorkOsWidgetIdentityResolver, MissingWorkOsWidgetIdentityResolver>();
        services.TryAddScoped<IWorkOsCurrentSessionIdResolver, HttpContextWorkOsCurrentSessionIdResolver>();
        services.AddHttpClient<IWorkOsWidgetTokenService, WorkOsWidgetTokenService>();

        return services;
    }
}
