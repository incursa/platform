namespace Incursa.Integrations.WorkOS.AspNetCore.DependencyInjection;

using Incursa.Integrations.WorkOS.AspNetCore.Auth;
using Incursa.Platform.Access.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class WorkOsAccessAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsAccessAspNetCore(
        this IServiceCollection services,
        Action<AccessAspNetCoreOptions>? configureAccess = null,
        Action<AccessSessionCookieOptions>? configureSessionCookies = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAccessAspNetCore(configureAccess);
        services.AddAccessCookieAuthentication(configureSessionCookies);
        services.Replace(ServiceDescriptor.Scoped<IAccessClaimsPrincipalFactory, WorkOsAccessClaimsPrincipalFactory>());
        return services;
    }
}
