namespace Incursa.Platform.Webhooks.WorkOS;

using Incursa.Platform.Webhooks.WorkOS.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Service registration helpers for the WorkOS webhook adapter.
/// </summary>
public static class WorkOsWebhookServiceCollectionExtensions
{
    /// <summary>
    /// Registers the thin WorkOS webhook provider adapter for the shared webhook pipeline.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional WorkOS webhook configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddWorkOsWebhooks(
        this IServiceCollection services,
        Action<WorkOsWebhookOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.TryAddSingleton(Options.Create(new WorkOsWebhookOptions()));
        }

        services.AddOptions<WorkOsWebhookOptions>();
        services.TryAddSingleton<WorkOsWebhookAuthenticator>();
        services.TryAddSingleton<WorkOsWebhookClassifier>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWebhookProvider, WorkOsWebhookProvider>());
        return services;
    }
}
