namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class AccessAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddAccessAspNetCore(
        this IServiceCollection services,
        Action<AccessAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AccessAspNetCoreOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddDataProtection();
        services.AddOptions<AccessSessionCookieOptions>()
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AccessSessionCookieOptions>, AccessSessionCookieOptionsValidation>());
        services.TryAddScoped<IAccessSessionStore, CookieAccessSessionStore>();
        services.TryAddScoped<IAccessClaimsPrincipalFactory, DefaultAccessClaimsPrincipalFactory>();
        services.TryAddScoped<IAccessAuthenticationTicketService, AccessAuthenticationTicketService>();
        services.TryAddScoped<ICurrentAccessContextAccessor, CurrentAccessContextAccessor>();
        return services;
    }

    public static IServiceCollection AddAccessCookieAuthentication(
        this IServiceCollection services,
        Action<AccessSessionCookieOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuredOptions = new AccessSessionCookieOptions();
        configure?.Invoke(configuredOptions);

        var optionsBuilder = services.AddOptions<AccessSessionCookieOptions>()
            .ValidateOnStart();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AccessSessionCookieOptions>, AccessSessionCookieOptionsValidation>());

        services.AddAuthentication(configuredOptions.AuthenticationScheme)
            .AddCookie(configuredOptions.AuthenticationScheme, options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = configuredOptions.AuthenticationCookieName;
                options.Cookie.Path = configuredOptions.CookiePath;
                options.Cookie.SameSite = configuredOptions.SameSite;
                options.Cookie.SecurePolicy = configuredOptions.SecurePolicy;
                options.ExpireTimeSpan = configuredOptions.ExpireTimeSpan;
                options.SlidingExpiration = configuredOptions.SlidingExpiration;
            });

        return services;
    }
}
