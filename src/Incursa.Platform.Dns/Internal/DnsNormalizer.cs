namespace Incursa.Platform.Dns.Internal;

using System.Globalization;

internal static class DnsNormalizer
{
    public static DnsZone NormalizeZone(DnsZone zone) =>
        new(
            zone.Id,
            NormalizeDomainName(zone.Name),
            zone.Owner,
            zone.CreatedUtc,
            zone.ExternalLinks);

    public static DnsRecord NormalizeRecord(DnsRecord record) =>
        new(
            record.Id,
            record.ZoneId,
            NormalizeDomainName(record.Name),
            record.Kind,
            NormalizeData(record.Kind, record.Data),
            record.Ttl,
            record.Proxied,
            record.Comment,
            record.CreatedUtc,
            record.ExternalLinks);

    public static string Signature(DnsRecord record)
    {
        var normalized = NormalizeRecord(record);
        return normalized.Kind + "|" + normalized.Name + "|" + normalized.Ttl.ToString(CultureInfo.InvariantCulture) + "|"
               + normalized.Proxied.ToString(CultureInfo.InvariantCulture) + "|"
               + (normalized.Comment ?? string.Empty) + "|"
               + normalized.Data switch
               {
                   DnsAddressRecordData address => address.Address,
                   DnsCanonicalNameRecordData canonicalName => canonicalName.CanonicalName,
                   DnsTextRecordData text => text.Text,
                   DnsMailExchangeRecordData mailExchange => mailExchange.Exchange + "|" + mailExchange.Preference.ToString(CultureInfo.InvariantCulture),
                   _ => throw new InvalidOperationException("Unknown DNS record data type."),
               };
    }

    public static string NormalizeDomainName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static DnsRecordData NormalizeData(DnsRecordKind kind, DnsRecordData data) =>
        kind switch
        {
            DnsRecordKind.A => NormalizeAddress(data),
            DnsRecordKind.Aaaa => NormalizeAddress(data),
            DnsRecordKind.CName => NormalizeCanonicalName(data),
            DnsRecordKind.Txt => NormalizeText(data),
            DnsRecordKind.Mx => NormalizeMailExchange(data),
            _ => throw new InvalidOperationException("Unknown DNS record kind."),
        };

    private static DnsRecordData NormalizeAddress(DnsRecordData data)
    {
        if (data is not DnsAddressRecordData address)
        {
            throw new InvalidOperationException("Address records require DnsAddressRecordData.");
        }

        return new DnsAddressRecordData(address.Address.Trim());
    }

    private static DnsRecordData NormalizeCanonicalName(DnsRecordData data)
    {
        if (data is not DnsCanonicalNameRecordData canonicalName)
        {
            throw new InvalidOperationException("CNAME records require DnsCanonicalNameRecordData.");
        }

        return new DnsCanonicalNameRecordData(NormalizeDomainName(canonicalName.CanonicalName));
    }

    private static DnsRecordData NormalizeText(DnsRecordData data)
    {
        if (data is not DnsTextRecordData text)
        {
            throw new InvalidOperationException("TXT records require DnsTextRecordData.");
        }

        return new DnsTextRecordData(text.Text.Trim());
    }

    private static DnsRecordData NormalizeMailExchange(DnsRecordData data)
    {
        if (data is not DnsMailExchangeRecordData mailExchange)
        {
            throw new InvalidOperationException("MX records require DnsMailExchangeRecordData.");
        }

        return new DnsMailExchangeRecordData(
            NormalizeDomainName(mailExchange.Exchange),
            mailExchange.Preference);
    }
}
