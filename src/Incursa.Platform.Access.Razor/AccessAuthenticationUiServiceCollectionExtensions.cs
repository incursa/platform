using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Access.Razor;

public static class AccessAuthenticationUiServiceCollectionExtensions
{
    public static IServiceCollection AddAccessAuthenticationUi(
        this IServiceCollection services,
        Action<AccessAuthenticationUiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AccessAuthenticationUiOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddScoped<AccessAuthenticationStateStore>();
        services.TryAddScoped<AccessAuthenticationFlowRouter>();
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<RazorPagesOptions>, AccessAuthenticationUiRazorPagesOptionsSetup>());

        return services;
    }
}
