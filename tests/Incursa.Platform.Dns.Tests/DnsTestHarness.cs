#pragma warning disable MA0048
namespace Incursa.Platform.Dns.Tests;

using Incursa.Platform.Dns;
using Incursa.Platform.Dns.Internal;
using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

internal sealed class DnsTestHarness : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    public DnsTestHarness()
    {
        var services = new ServiceCollection();
        AddStorage(services, new InMemoryRecordStore<DnsZone>());
        AddStorage(services, new InMemoryRecordStore<DnsRecord>());
        AddStorage(services, new InMemoryLookupStore<ZoneByOwnerProjection>());
        AddStorage(services, new InMemoryLookupStore<RecordByZoneProjection>());
        services.AddDns();

        serviceProvider = services.BuildServiceProvider();
    }

    public IDnsZoneService Zones => serviceProvider.GetRequiredService<IDnsZoneService>();

    public IDnsRecordService Records => serviceProvider.GetRequiredService<IDnsRecordService>();

    public IDnsQueryService Query => serviceProvider.GetRequiredService<IDnsQueryService>();

    public static DnsZone CreateZone(
        string id,
        string name = "example.com",
        string? owner = null,
        DateTimeOffset? createdUtc = null,
        IReadOnlyCollection<DnsExternalLink>? externalLinks = null) =>
        new(new DnsZoneId(id), name, owner, createdUtc, externalLinks);

    public static DnsRecord CreateRecord(
        string id,
        string zoneId,
        string name,
        DnsRecordKind kind,
        DnsRecordData data,
        int ttl = 300,
        bool proxied = false,
        string? comment = null,
        DateTimeOffset? createdUtc = null,
        IReadOnlyCollection<DnsExternalLink>? externalLinks = null) =>
        new(new DnsRecordId(id), new DnsZoneId(zoneId), name, kind, data, ttl, proxied, comment, createdUtc, externalLinks);

    public static async Task<IReadOnlyCollection<TItem>> ToListAsync<TItem>(
        IAsyncEnumerable<TItem> source,
        CancellationToken cancellationToken = default)
    {
        List<TItem> items = [];
        await foreach (var item in source.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(item);
        }

        return items;
    }

    public void Dispose() => serviceProvider.Dispose();

    private static void AddStorage<TRecord>(IServiceCollection services, InMemoryRecordStore<TRecord> store)
        where TRecord : class =>
        services.AddSingleton<IRecordStore<TRecord>>(store);

    private static void AddStorage<TLookup>(IServiceCollection services, InMemoryLookupStore<TLookup> store)
        where TLookup : class =>
        services.AddSingleton<ILookupStore<TLookup>>(store);
}
#pragma warning restore MA0048
