namespace Incursa.Integrations.WorkOS.Access;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Platform.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public static class WorkOsAccessServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsAccess(
        this IServiceCollection services,
        Action<WorkOsAccessOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.TryAddSingleton(Options.Create(new WorkOsAccessOptions()));
        }

        services.TryAddSingleton<IWorkOsAccessSynchronizationService, WorkOsAccessSynchronizationService>();
        return services;
    }

    public static IServiceCollection AddWorkOsAuthentication(
        this IServiceCollection services,
        Action<WorkOsAuthOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddMemoryCache();
        services.AddOptions<WorkOsAuthOptions>()
            .Configure(configure)
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WorkOsAuthOptions>, WorkOsAuthOptionsValidation>());

        services.AddHttpClient<WorkOsAuthenticationClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WorkOsAuthOptions>>().Value;
            client.Timeout = options.RequestTimeout;
        });

        services.AddHttpClient<IWorkOsTokenValidator, WorkOsTokenValidator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WorkOsAuthOptions>>().Value;
            client.Timeout = options.RequestTimeout;
        });

        services.TryAddTransient<IWorkOsAuthenticationClient>(static sp => sp.GetRequiredService<WorkOsAuthenticationClient>());
        services.TryAddTransient<IWorkOsMagicAuthClient>(static sp => sp.GetRequiredService<WorkOsAuthenticationClient>());
        services.TryAddTransient<IWorkOsSessionClient>(static sp => sp.GetRequiredService<WorkOsAuthenticationClient>());
        services.TryAddTransient<IWorkOsPasswordResetClient>(static sp => sp.GetRequiredService<WorkOsAuthenticationClient>());
        services.TryAddScoped<IAccessAuthenticationService, WorkOsAccessAuthenticationService>();
        return services;
    }
}
