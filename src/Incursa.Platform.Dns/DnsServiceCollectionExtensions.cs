namespace Incursa.Platform.Dns;

using Incursa.Platform.Dns.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class DnsServiceCollectionExtensions
{
    public static IServiceCollection AddDns(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<DnsStorageContext>();
        services.TryAddSingleton<IDnsZoneService, StorageBackedDnsZoneService>();
        services.TryAddSingleton<IDnsRecordService, StorageBackedDnsRecordService>();
        services.TryAddSingleton<IDnsQueryService, StorageBackedDnsQueryService>();

        return services;
    }
}
