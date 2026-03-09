namespace Incursa.Integrations.WorkOS.Core.DependencyInjection;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Audit;
using Incursa.Integrations.WorkOS.Abstractions.Authorization;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Abstractions.Mapping;
using Incursa.Integrations.WorkOS.Abstractions.Persistence;
using Incursa.Integrations.WorkOS.Abstractions.Profiles;
using Incursa.Integrations.WorkOS.Abstractions.Telemetry;
using Incursa.Integrations.WorkOS.Abstractions.Webhooks;
using Incursa.Integrations.WorkOS.Core.Authentication;
using Incursa.Integrations.WorkOS.Core.Authorization;
using Incursa.Integrations.WorkOS.Core.Claims;
using Incursa.Integrations.WorkOS.Core.Clients;
using Incursa.Integrations.WorkOS.Core.Emulation;
using Incursa.Integrations.WorkOS.Core.Management;
using Incursa.Integrations.WorkOS.Core.Mapping;
using Incursa.Integrations.WorkOS.Core.Profiles;
using Incursa.Integrations.WorkOS.Core.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsInMemoryPlatform(this IServiceCollection services, Action<IInMemoryWorkOsState>? seed = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<InMemoryWorkOsState>(_ =>
        {
            var state = new InMemoryWorkOsState();
            seed?.Invoke(state);
            return state;
        });
        services.AddSingleton<IInMemoryWorkOsState>(static sp => sp.GetRequiredService<InMemoryWorkOsState>());

        services.Replace(ServiceDescriptor.Singleton<IWorkOsManagementClient>(static sp => sp.GetRequiredService<InMemoryWorkOsManagementClient>()));
        services.Replace(ServiceDescriptor.Singleton<IWorkOsMembershipClient>(static sp => sp.GetRequiredService<InMemoryWorkOsMembershipClient>()));
        services.Replace(ServiceDescriptor.Singleton<IWorkOsTenantMapper>(static sp => sp.GetRequiredService<InMemoryWorkOsTenantMapper>()));

        services.AddSingleton<InMemoryWorkOsManagementClient>();
        services.AddSingleton<InMemoryWorkOsMembershipClient>();
        services.AddSingleton<InMemoryWorkOsTenantMapper>();

        return services;
    }

    public static IServiceCollection AddWorkOsIntegrationCore(
        this IServiceCollection services,
        Action<WorkOsIntegrationOptions> configureOptions,
        Action<WorkOsPermissionMappingOptions>? configurePermissionMapping = null,
        Action<WorkOsUserProfileHydrationOptions>? configureUserProfileHydration = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var integrationOptions = new WorkOsIntegrationOptions();
        configureOptions(integrationOptions);

        return services.AddWorkOsIntegrationCore(
            configureOptions: _ => { },
            configureManagement: management =>
            {
                management.BaseUrl = integrationOptions.BaseUrl;
                management.ApiKey = integrationOptions.ApiKey;
                management.RequestTimeout = integrationOptions.RequestTimeout;
                management.RetryCount = integrationOptions.RetryCount;
            },
            configurePermissionMapping: configurePermissionMapping,
            configureClientCredentials: null,
            configureClaimsEnrichment: null,
            configureUserProfileHydration: configureUserProfileHydration,
            preconfiguredIntegrationOptions: integrationOptions);
    }

    public static IServiceCollection AddWorkOsIntegrationCore(
        this IServiceCollection services,
        Action<WorkOsIntegrationOptions> configureOptions,
        Action<WorkOsManagementOptions> configureManagement,
        Action<WorkOsPermissionMappingOptions>? configurePermissionMapping = null,
        Action<WorkOsClientCredentialsOptions>? configureClientCredentials = null,
        Action<WorkOsClaimsEnrichmentOptions>? configureClaimsEnrichment = null,
        Action<WorkOsUserProfileHydrationOptions>? configureUserProfileHydration = null)
    {
        return services.AddWorkOsIntegrationCore(
            configureOptions,
            configureManagement,
            configurePermissionMapping,
            configureClientCredentials,
            configureClaimsEnrichment,
            configureUserProfileHydration,
            preconfiguredIntegrationOptions: null);
    }

    private static IServiceCollection AddWorkOsIntegrationCore(
        this IServiceCollection services,
        Action<WorkOsIntegrationOptions> configureOptions,
        Action<WorkOsManagementOptions> configureManagement,
        Action<WorkOsPermissionMappingOptions>? configurePermissionMapping,
        Action<WorkOsClientCredentialsOptions>? configureClientCredentials,
        Action<WorkOsClaimsEnrichmentOptions>? configureClaimsEnrichment,
        Action<WorkOsUserProfileHydrationOptions>? configureUserProfileHydration,
        WorkOsIntegrationOptions? preconfiguredIntegrationOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        ArgumentNullException.ThrowIfNull(configureManagement);

        var options = preconfiguredIntegrationOptions ?? new WorkOsIntegrationOptions();
        if (preconfiguredIntegrationOptions is null)
        {
            configureOptions(options);
        }

        var managementOptions = new WorkOsManagementOptions();
        configureManagement(managementOptions);

        var claimsOptions = new WorkOsClaimsEnrichmentOptions();
        configureClaimsEnrichment?.Invoke(claimsOptions);
        var userProfileHydrationOptions = new WorkOsUserProfileHydrationOptions();
        configureUserProfileHydration?.Invoke(userProfileHydrationOptions);

        var permissionOptions = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        configurePermissionMapping?.Invoke(permissionOptions);

        var clientCredentialsOptions = new WorkOsClientCredentialsOptions();
        configureClientCredentials?.Invoke(clientCredentialsOptions);

        services.AddSingleton(options);
        services.AddSingleton(managementOptions);
        services.AddSingleton(permissionOptions);
        services.AddSingleton(claimsOptions);
        services.AddSingleton(userProfileHydrationOptions);
        services.AddSingleton(clientCredentialsOptions);
        services.AddSingleton<WorkOsPermissionMapper>();
        services.AddMemoryCache();
        services.AddSingleton<IWorkOsIntegrationTelemetry>(NullWorkOsIntegrationTelemetry.Instance);
        services.TryAddSingleton<IWorkOsTenantMapper, WorkOsTenantMapper>();
        services.TryAddSingleton<IWorkOsUserProfileCache, MemoryWorkOsUserProfileCache>();
        services.TryAddSingleton<IWorkOsUserProfileProvider, WorkOsUserProfileProvider>();
        services.TryAddSingleton<IWorkOsUserProfileProjector, WorkOsUserProfileProjector>();

        services.AddHttpClient<IWorkOsMembershipClient, WorkOsMembershipClient>(static (sp, httpClient) =>
        {
            var management = sp.GetRequiredService<WorkOsManagementOptions>();
            httpClient.BaseAddress = new Uri(management.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            httpClient.Timeout = management.RequestTimeout;
        });
        services.AddHttpClient<IWorkOsManagementClient, WorkOsManagementHttpClient>();
        services.AddHttpClient<IWorkOsAuditClient, WorkOsAuditHttpClient>();
        services.AddHttpClient<IWorkOsAccessTokenProvider, WorkOsClientCredentialsTokenProvider>();
        services.AddSingleton<IWorkOsClaimsEnricher>(static sp =>
            new WorkOsClaimsEnricher(
                sp.GetRequiredService<IWorkOsUserProfileProvider>(),
                sp.GetRequiredService<IWorkOsUserProfileProjector>(),
                sp.GetRequiredService<WorkOsClaimsEnrichmentOptions>()));
        services.AddSingleton<IWorkOsApiKeyAuthenticator, WorkOsApiKeyAuthenticator>();
        services.AddSingleton<IWorkOsScopeAuthorizer, WorkOsScopeAuthorizer>();
        services.AddSingleton<IWorkOsApiKeyManager, WorkOsApiKeyManager>();
        services.AddSingleton<IWorkOsManagementAuthorizer, WorkOsManagementAuthorizer>();

        services.AddSingleton<IWorkOsWebhookVerifier>(sp => new WorkOsWebhookVerifier(sp.GetRequiredService<WorkOsIntegrationOptions>().WebhookSigningSecret));
        services.AddSingleton<IWorkOsWebhookProcessor>(sp =>
            new WorkOsWebhookProcessor(
                sp.GetRequiredService<IWorkOsWebhookEventDedupStore>(),
                sp.GetServices<IWorkOsWebhookEventHandler>(),
                TimeSpan.FromHours(24),
                sp.GetService<IWorkOsIntegrationTelemetry>()));

        return services;
    }
}

