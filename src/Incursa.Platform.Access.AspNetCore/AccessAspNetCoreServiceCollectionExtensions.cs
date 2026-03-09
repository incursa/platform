namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.TryAddScoped<ICurrentAccessContextAccessor, CurrentAccessContextAccessor>();
        return services;
    }
}
