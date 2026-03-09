namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.DependencyInjection;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsAppAuth(
        this IServiceCollection services,
        Action<WorkOsAppAuthOptions>? configureOptions = null,
        string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<WorkOsAppAuthOptions>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<WorkOsAppAuthOptions>, PermissionPolicyPrefixPostConfigure>());

        services.TryAddScoped<OrganizationContextAccessor>();
        services.TryAddScoped<IOrganizationContextAccessor>(static sp => sp.GetRequiredService<OrganizationContextAccessor>());
        services.TryAddScoped<IOrganizationContextSetter>(static sp => sp.GetRequiredService<OrganizationContextAccessor>());
        services.TryAddScoped<IWorkOsClaimsAccessor, DefaultWorkOsClaimsAccessor>();
        services.TryAddScoped<IOrganizationSelectionStore, CookieOrganizationSelectionStore>();
        services.TryAddScoped<IWorkOsOrganizationMembershipResolver, PassThroughOrganizationMembershipResolver>();
        services.TryAddScoped<OrganizationContextMiddleware>();
        services.TryAddScoped<RequireOrganizationSelectionMiddleware>();

        services.AddAuthentication(cookieScheme)
            .AddCookie(cookieScheme);

        services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, AnyPermissionHandler>());

        return services;
    }
}
