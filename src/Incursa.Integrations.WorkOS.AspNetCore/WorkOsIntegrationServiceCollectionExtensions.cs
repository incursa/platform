namespace Incursa.Integrations.WorkOS;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Incursa.Integrations.WorkOS.AppAuth.AspNetCore.DependencyInjection;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Access;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.DependencyInjection;
using Incursa.Integrations.WorkOS.Core.DependencyInjection;
using Incursa.Integrations.WorkOS.Core.Emulation;
using Incursa.Integrations.WorkOS.Persistence.DependencyInjection;
using Incursa.Platform.Access.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

public static class WorkOsIntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsIntegration(
        this IServiceCollection services,
        Action<WorkOsIntegrationOptions> configureOptions,
        Action<WorkOsPermissionMappingOptions>? configurePermissionMapping = null,
        Action<WorkOsUserProfileHydrationOptions>? configureUserProfileHydration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddWorkOsInMemoryPersistence();
        services.AddWorkOsIntegrationCore(configureOptions, configurePermissionMapping, configureUserProfileHydration);
        services.AddWorkOsAspNetCore();
        return services;
    }

    public static IServiceCollection AddWorkOsIntegration(
        this IServiceCollection services,
        Action<WorkOsIntegrationOptions> configureOptions,
        Action<WorkOsManagementOptions> configureManagement,
        Action<WorkOsPermissionMappingOptions>? configurePermissionMapping = null,
        Action<WorkOsClientCredentialsOptions>? configureClientCredentials = null,
        Action<WorkOsClaimsEnrichmentOptions>? configureClaimsEnrichment = null,
        Action<WorkOsUserProfileHydrationOptions>? configureUserProfileHydration = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddWorkOsInMemoryPersistence();
        services.AddWorkOsIntegrationCore(
            configureOptions,
            configureManagement,
            configurePermissionMapping,
            configureClientCredentials,
            configureClaimsEnrichment,
            configureUserProfileHydration);
        services.AddWorkOsAspNetCore();
        return services;
    }

    public static IServiceCollection AddWorkOsOidcAuthKit(
        this IServiceCollection services,
        Action<WorkOsOidcOptions> configureOptions,
        string openIdConnectScheme = "workos",
        string cookieScheme = "Cookies")
    {
        return AspNetCore.DependencyInjection.ServiceCollectionExtensions.AddWorkOsOidcAuthKit(
            services,
            configureOptions,
            openIdConnectScheme,
            cookieScheme);
    }

    public static IServiceCollection AddWorkOsAppAuth(
        this IServiceCollection services,
        Action<WorkOsAppAuthOptions>? configureOptions = null,
        string cookieScheme = "Cookies")
    {
        ArgumentNullException.ThrowIfNull(services);
        return Incursa.Integrations.WorkOS.AppAuth.AspNetCore.DependencyInjection.ServiceCollectionExtensions
            .AddWorkOsAppAuth(services, configureOptions, cookieScheme);
    }

    public static IServiceCollection AddWorkOsCustomUiAuthentication(
        this IServiceCollection services,
        Action<WorkOsAuthOptions> configureAuth,
        Action<AccessAspNetCoreOptions>? configureAccess = null,
        Action<AccessSessionCookieOptions>? configureCookie = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureAuth);

        services.AddWorkOsAuthentication(configureAuth);
        services.AddWorkOsAccessAspNetCore(configureAccess, configureCookie);
        return services;
    }

    public static IServiceCollection AddWorkOsInMemoryIntegration(
        this IServiceCollection services,
        Action<WorkOsIntegrationOptions> configureOptions,
        Action<IInMemoryWorkOsState>? seed = null,
        Action<WorkOsPermissionMappingOptions>? configurePermissionMapping = null,
        Action<WorkOsUserProfileHydrationOptions>? configureUserProfileHydration = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddWorkOsInMemoryPersistence();
        services.AddWorkOsIntegrationCore(configureOptions, configurePermissionMapping, configureUserProfileHydration);
        services.AddWorkOsInMemoryPlatform(seed);
        services.AddWorkOsAspNetCore();
        return services;
    }

    public static IServiceCollection AddWorkOsWidgets(
        this IServiceCollection services,
        Action<WorkOsWidgetsOptions> configureOptions)
    {
        return WorkOsWidgetsServiceCollectionExtensions.AddWorkOsWidgets(services, configureOptions);
    }

    public static IServiceCollection AddWorkOsWidgets(
        this IServiceCollection services,
        Action<WorkOsWidgetsOptions> configureOptions,
        Func<IServiceProvider, IWorkOsWidgetIdentityResolver> identityResolverFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        ArgumentNullException.ThrowIfNull(identityResolverFactory);

        services.AddWorkOsWidgets(configureOptions);
        services.AddScoped(identityResolverFactory);
        return services;
    }

    public static IServiceCollection AddWorkOsWidgets(
        this IServiceCollection services,
        Action<WorkOsWidgetsOptions> configureOptions,
        Func<IServiceProvider, IWorkOsWidgetIdentityResolver> identityResolverFactory,
        Func<IServiceProvider, IWorkOsCurrentSessionIdResolver> currentSessionIdResolverFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);
        ArgumentNullException.ThrowIfNull(identityResolverFactory);
        ArgumentNullException.ThrowIfNull(currentSessionIdResolverFactory);

        services.AddWorkOsWidgets(configureOptions, identityResolverFactory);
        services.AddScoped(currentSessionIdResolverFactory);
        return services;
    }
}
