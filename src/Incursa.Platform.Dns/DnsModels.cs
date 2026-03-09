#pragma warning disable MA0048
namespace Incursa.Platform.Dns;

public enum DnsRecordKind
{
    A = 0,
    Aaaa = 1,
    CName = 2,
    Txt = 3,
    Mx = 4,
}

public sealed record DnsExternalLink
{
    public DnsExternalLink(DnsExternalLinkId id, string provider, string externalId, string? resourceType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        Id = id;
        Provider = provider.Trim();
        ExternalId = externalId.Trim();
        ResourceType = string.IsNullOrWhiteSpace(resourceType) ? null : resourceType.Trim();
    }

    public DnsExternalLinkId Id { get; }

    public string Provider { get; }

    public string ExternalId { get; }

    public string? ResourceType { get; }
}

public sealed record DnsZone
{
    public DnsZone(
        DnsZoneId id,
        string name,
        string? owner = null,
        DateTimeOffset? createdUtc = null,
        IReadOnlyCollection<DnsExternalLink>? externalLinks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
        Owner = string.IsNullOrWhiteSpace(owner) ? null : owner.Trim();
        CreatedUtc = createdUtc;
        ExternalLinks = externalLinks is null ? Array.Empty<DnsExternalLink>() : externalLinks.ToArray();
    }

    public DnsZoneId Id { get; }

    public string Name { get; }

    public string? Owner { get; }

    public DateTimeOffset? CreatedUtc { get; }

    public IReadOnlyCollection<DnsExternalLink> ExternalLinks { get; }
}

public abstract record DnsRecordData;

public sealed record DnsAddressRecordData(string Address) : DnsRecordData;

public sealed record DnsCanonicalNameRecordData(string CanonicalName) : DnsRecordData;

public sealed record DnsTextRecordData(string Text) : DnsRecordData;

public sealed record DnsMailExchangeRecordData(string Exchange, int Preference) : DnsRecordData;

public sealed record DnsRecord
{
    public DnsRecord(
        DnsRecordId id,
        DnsZoneId zoneId,
        string name,
        DnsRecordKind kind,
        DnsRecordData data,
        int ttl = 300,
        bool proxied = false,
        string? comment = null,
        DateTimeOffset? createdUtc = null,
        IReadOnlyCollection<DnsExternalLink>? externalLinks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(data);

        if (ttl <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive.");
        }

        Id = id;
        ZoneId = zoneId;
        Name = name.Trim();
        Kind = kind;
        Data = data;
        Ttl = ttl;
        Proxied = proxied;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        CreatedUtc = createdUtc;
        ExternalLinks = externalLinks is null ? Array.Empty<DnsExternalLink>() : externalLinks.ToArray();
    }

    public DnsRecordId Id { get; }

    public DnsZoneId ZoneId { get; }

    public string Name { get; }

    public DnsRecordKind Kind { get; }

    public DnsRecordData Data { get; }

    public int Ttl { get; }

    public bool Proxied { get; }

    public string? Comment { get; }

    public DateTimeOffset? CreatedUtc { get; }

    public IReadOnlyCollection<DnsExternalLink> ExternalLinks { get; }
}

public sealed record DnsRecordQuery(DnsZoneId ZoneId, string? Name = null, DnsRecordKind? Kind = null);

public sealed record DnsReconcileResult(
    IReadOnlyCollection<DnsRecord> UpsertedRecords,
    IReadOnlyCollection<DnsRecord> DeletedRecords);
#pragma warning restore MA0048
