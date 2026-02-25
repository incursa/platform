namespace Incursa.Platform.Audit.WorkOS;

using Incursa.Platform;
using Incursa.Platform.Audit.WorkOS.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class WorkOsAuditSinkServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsAuditSink(
        this IServiceCollection services,
        Action<WorkOsAuditSinkOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new WorkOsAuditSinkOptions();
        configure(options);

        services.AddSingleton(options);
        services.TryAddSingleton<IWorkOsAuditOrganizationResolver>(NullWorkOsAuditOrganizationResolver.Instance);
        services.AddSingleton<IAuditOutboxSinkSerializer, WorkOsAuditOutboxSerializer>();
        services.AddHttpClient<IWorkOsAuditPublisher, HttpWorkOsAuditPublisher>();
        services.AddOutboxHandler<WorkOsAuditOutboxHandler>();
        return services;
    }
}
