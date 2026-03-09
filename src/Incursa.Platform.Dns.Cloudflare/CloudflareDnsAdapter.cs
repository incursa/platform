#pragma warning disable MA0048
namespace Incursa.Platform.Dns.Cloudflare;

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Incursa.Platform.Dns;
using Microsoft.Extensions.DependencyInjection;

internal static class CloudflareDnsConstants
{
    public const string ProviderName = "cloudflare";
    public const string ZoneResourceType = "zone";
    public const string RecordResourceType = "dns-record";
}

public sealed class CloudflareDnsOptions
{
    public Uri BaseUrl { get; set; } = new("https://api.cloudflare.com/client/v4/", UriKind.Absolute);

    public string ApiToken { get; set; } = string.Empty;

    public string? ZoneId { get; set; }
}

public interface ICloudflareDnsAdapter
{
    Task<DnsZone> GetZoneAsync(string zoneId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DnsRecord>> ListRecordsAsync(DnsZone zone, CancellationToken cancellationToken = default);

    Task<DnsRecord> UpsertRecordAsync(DnsZone zone, DnsRecord record, CancellationToken cancellationToken = default);

    Task<bool> DeleteRecordAsync(DnsZone zone, DnsRecord record, CancellationToken cancellationToken = default);

    Task<DnsReconcileResult> ReconcileAsync(
        DnsZone zone,
        IReadOnlyCollection<DnsRecord> desiredRecords,
        CancellationToken cancellationToken = default);
}

public static class CloudflareDnsServiceCollectionExtensions
{
    public static IServiceCollection AddCloudflareDns(
        this IServiceCollection services,
        Action<CloudflareDnsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CloudflareDnsOptions();
        configure(options);
        ValidateOptions(options);

        services.AddSingleton(options);
        services.AddHttpClient<ICloudflareDnsAdapter, CloudflareDnsAdapter>((_, client) =>
        {
            client.BaseAddress = options.BaseUrl;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
        });

        return services;
    }

    private static void ValidateOptions(CloudflareDnsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ApiToken))
        {
            throw new InvalidOperationException("Cloudflare DNS options must define an API token.");
        }
    }
}

internal sealed class CloudflareDnsAdapter : ICloudflareDnsAdapter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly CloudflareDnsOptions options;

    public CloudflareDnsAdapter(HttpClient httpClient, CloudflareDnsOptions options)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<DnsZone> GetZoneAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "zones/" + Uri.EscapeDataString(zoneId.Trim()));
        var zone = await SendAsync<CloudflareZoneModel>(request, cancellationToken).ConfigureAwait(false);
        var externalZoneId = zone.Id ?? zoneId.Trim();

        return new DnsZone(
            new DnsZoneId(externalZoneId),
            NormalizeDomainName(zone.Name ?? zoneId.Trim()),
            externalLinks:
            [
                new DnsExternalLink(
                    new DnsExternalLinkId("cf-zone:" + Uri.EscapeDataString(externalZoneId)),
                    CloudflareDnsConstants.ProviderName,
                    externalZoneId,
                    CloudflareDnsConstants.ZoneResourceType),
            ]);
    }

    public async Task<IReadOnlyCollection<DnsRecord>> ListRecordsAsync(
        DnsZone zone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zone);

        var zoneId = ResolveZoneId(zone);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "zones/" + Uri.EscapeDataString(zoneId) + "/dns_records?per_page=5000");

        var response = await SendAsync<List<CloudflareDnsRecordModel>>(request, cancellationToken).ConfigureAwait(false);
        return response
            .Where(static item => item.Type is not null)
            .Select(item => MapRecord(zone.Id, item))
            .Where(static item => item is not null)
            .Cast<DnsRecord>()
            .ToArray();
    }

    public async Task<DnsRecord> UpsertRecordAsync(
        DnsZone zone,
        DnsRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(record);

        var zoneId = ResolveZoneId(zone);
        var existingRecordId = ResolveRecordId(record);
        if (string.IsNullOrWhiteSpace(existingRecordId))
        {
            var existing = await ListRecordsAsync(zone, cancellationToken).ConfigureAwait(false);
            existingRecordId = ResolveCandidateRecordId(existing, record);
        }

        var body = SerializeRecord(record);
        using var request = new HttpRequestMessage(
            string.IsNullOrWhiteSpace(existingRecordId) ? HttpMethod.Post : HttpMethod.Put,
            string.IsNullOrWhiteSpace(existingRecordId)
                ? "zones/" + Uri.EscapeDataString(zoneId) + "/dns_records"
                : "zones/" + Uri.EscapeDataString(zoneId) + "/dns_records/" + Uri.EscapeDataString(existingRecordId))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        var response = await SendAsync<CloudflareDnsRecordModel>(request, cancellationToken).ConfigureAwait(false);
        return MapRecord(zone.Id, response) ?? throw new InvalidOperationException("Cloudflare returned an unsupported DNS record.");
    }

    public async Task<bool> DeleteRecordAsync(
        DnsZone zone,
        DnsRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(record);

        var zoneId = ResolveZoneId(zone);
        var recordId = ResolveRecordId(record);
        if (string.IsNullOrWhiteSpace(recordId))
        {
            var existing = await ListRecordsAsync(zone, cancellationToken).ConfigureAwait(false);
            recordId = ResolveCandidateRecordId(existing, record);
        }

        if (string.IsNullOrWhiteSpace(recordId))
        {
            return false;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            "zones/" + Uri.EscapeDataString(zoneId) + "/dns_records/" + Uri.EscapeDataString(recordId));
        _ = await SendAsync<JsonElement>(request, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<DnsReconcileResult> ReconcileAsync(
        DnsZone zone,
        IReadOnlyCollection<DnsRecord> desiredRecords,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(desiredRecords);

        var existing = await ListRecordsAsync(zone, cancellationToken).ConfigureAwait(false);
        Dictionary<string, DnsRecord> desiredBySignature = new(StringComparer.Ordinal);
        foreach (var desired in desiredRecords)
        {
            if (desired.ZoneId != zone.Id)
            {
                throw new InvalidOperationException("All desired records must target the same zone.");
            }

            _ = desiredBySignature.TryAdd(Signature(desired), desired);
        }

        var existingBySignature = existing.ToDictionary(Signature, StringComparer.Ordinal);
        List<DnsRecord> upserted = [];
        List<DnsRecord> deleted = [];

        foreach (var pair in desiredBySignature)
        {
            var desired = pair.Value;
            if (existingBySignature.TryGetValue(pair.Key, out var current))
            {
                desired = new DnsRecord(
                    current.Id,
                    desired.ZoneId,
                    desired.Name,
                    desired.Kind,
                    desired.Data,
                    desired.Ttl,
                    desired.Proxied,
                    desired.Comment,
                    current.CreatedUtc,
                    desired.ExternalLinks.Count == 0 ? current.ExternalLinks : desired.ExternalLinks);
            }

            upserted.Add(await UpsertRecordAsync(zone, desired, cancellationToken).ConfigureAwait(false));
        }

        foreach (var existingRecord in existing)
        {
            if (desiredBySignature.ContainsKey(Signature(existingRecord)))
            {
                continue;
            }

            if (await DeleteRecordAsync(zone, existingRecord, cancellationToken).ConfigureAwait(false))
            {
                deleted.Add(existingRecord);
            }
        }

        return new DnsReconcileResult(upserted, deleted);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                "Cloudflare request failed with status code "
                + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)
                + ".");
        }

        var envelope = JsonSerializer.Deserialize<CloudflareEnvelope<T>>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Cloudflare returned an empty payload.");
        if (!envelope.Success)
        {
            throw new InvalidOperationException(FormatErrors(envelope.Errors));
        }

        return envelope.Result ?? throw new InvalidOperationException("Cloudflare returned no result payload.");
    }

    private string ResolveZoneId(DnsZone zone)
    {
        var zoneId = zone.ExternalLinks.FirstOrDefault(IsCloudflareZoneLink)?.ExternalId;
        zoneId ??= options.ZoneId;

        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new InvalidOperationException("A Cloudflare zone id is required.");
        }

        return zoneId.Trim();
    }

    private static string? ResolveRecordId(DnsRecord record) =>
        record.ExternalLinks.FirstOrDefault(IsCloudflareRecordLink)?.ExternalId;

    private static string? ResolveCandidateRecordId(
        IReadOnlyCollection<DnsRecord> existingRecords,
        DnsRecord desiredRecord)
    {
        var desiredSignature = Signature(desiredRecord);
        var signatureMatch = existingRecords.FirstOrDefault(
            item => string.Equals(Signature(item), desiredSignature, StringComparison.Ordinal));
        if (signatureMatch is not null)
        {
            return ResolveRecordId(signatureMatch);
        }

        var normalizedName = NormalizeDomainName(desiredRecord.Name);
        var kindMatches = existingRecords
            .Where(item => string.Equals(item.Name, normalizedName, StringComparison.Ordinal) && item.Kind == desiredRecord.Kind)
            .ToArray();

        return kindMatches.Length == 1 ? ResolveRecordId(kindMatches[0]) : null;
    }

    private static bool IsCloudflareZoneLink(DnsExternalLink link) =>
        string.Equals(link.Provider, CloudflareDnsConstants.ProviderName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(link.ResourceType, CloudflareDnsConstants.ZoneResourceType, StringComparison.OrdinalIgnoreCase);

    private static bool IsCloudflareRecordLink(DnsExternalLink link) =>
        string.Equals(link.Provider, CloudflareDnsConstants.ProviderName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(link.ResourceType, CloudflareDnsConstants.RecordResourceType, StringComparison.OrdinalIgnoreCase);

    private static string SerializeRecord(DnsRecord record)
    {
        var normalizedName = NormalizeDomainName(record.Name);

        object payload = record.Data switch
        {
            DnsAddressRecordData address when record.Kind == DnsRecordKind.A || record.Kind == DnsRecordKind.Aaaa => new CloudflareDnsRecordRequest(
                record.Kind == DnsRecordKind.A ? "A" : "AAAA",
                normalizedName,
                address.Address.Trim(),
                record.Ttl,
                record.Proxied,
                record.Comment,
                null),
            DnsCanonicalNameRecordData canonicalName when record.Kind == DnsRecordKind.CName => new CloudflareDnsRecordRequest(
                "CNAME",
                normalizedName,
                NormalizeDomainName(canonicalName.CanonicalName),
                record.Ttl,
                record.Proxied,
                record.Comment,
                null),
            DnsTextRecordData text when record.Kind == DnsRecordKind.Txt => new CloudflareDnsRecordRequest(
                "TXT",
                normalizedName,
                text.Text.Trim(),
                record.Ttl,
                record.Proxied,
                record.Comment,
                null),
            DnsMailExchangeRecordData mailExchange when record.Kind == DnsRecordKind.Mx => new CloudflareDnsRecordRequest(
                "MX",
                normalizedName,
                NormalizeDomainName(mailExchange.Exchange),
                record.Ttl,
                record.Proxied,
                record.Comment,
                mailExchange.Preference),
            _ => throw new InvalidOperationException("Unsupported DNS record type for Cloudflare."),
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static DnsRecord? MapRecord(DnsZoneId zoneId, CloudflareDnsRecordModel model)
    {
        DnsRecordKind? kind = model.Type?.ToUpperInvariant() switch
        {
            "A" => DnsRecordKind.A,
            "AAAA" => DnsRecordKind.Aaaa,
            "CNAME" => DnsRecordKind.CName,
            "TXT" => DnsRecordKind.Txt,
            "MX" => DnsRecordKind.Mx,
            _ => null,
        };

        if (kind is null || string.IsNullOrWhiteSpace(model.Id) || string.IsNullOrWhiteSpace(model.Name))
        {
            return null;
        }

        var recordKind = kind.Value;
        DnsRecordData data = recordKind switch
        {
            DnsRecordKind.A => new DnsAddressRecordData((model.Content ?? string.Empty).Trim()),
            DnsRecordKind.Aaaa => new DnsAddressRecordData((model.Content ?? string.Empty).Trim()),
            DnsRecordKind.CName => new DnsCanonicalNameRecordData(NormalizeDomainName(model.Content ?? string.Empty)),
            DnsRecordKind.Txt => new DnsTextRecordData((model.Content ?? string.Empty).Trim()),
            DnsRecordKind.Mx => new DnsMailExchangeRecordData(NormalizeDomainName(model.Content ?? string.Empty), model.Priority ?? 0),
            _ => throw new InvalidOperationException("Unsupported Cloudflare DNS kind."),
        };

        return new DnsRecord(
            new DnsRecordId("cf-record:" + Uri.EscapeDataString(model.Id)),
            zoneId,
            NormalizeDomainName(model.Name),
            recordKind,
            data,
            model.Ttl <= 0 ? 300 : model.Ttl,
            model.Proxied ?? false,
            model.Comment,
            externalLinks:
            [
                new DnsExternalLink(
                    new DnsExternalLinkId("cf-record-link:" + Uri.EscapeDataString(model.Id)),
                    CloudflareDnsConstants.ProviderName,
                    model.Id,
                    CloudflareDnsConstants.RecordResourceType),
            ]);
    }

    private static string Signature(DnsRecord record)
    {
        var normalizedName = NormalizeDomainName(record.Name);
        return record.Kind + "|" + normalizedName + "|" + record.Ttl.ToString(CultureInfo.InvariantCulture) + "|"
               + record.Proxied.ToString(CultureInfo.InvariantCulture) + "|"
               + (record.Comment ?? string.Empty) + "|"
               + record.Data switch
               {
                   DnsAddressRecordData address => address.Address.Trim(),
                   DnsCanonicalNameRecordData canonicalName => NormalizeDomainName(canonicalName.CanonicalName),
                   DnsTextRecordData text => text.Text.Trim(),
                   DnsMailExchangeRecordData mailExchange => NormalizeDomainName(mailExchange.Exchange) + "|" + mailExchange.Preference.ToString(CultureInfo.InvariantCulture),
                   _ => throw new InvalidOperationException("Unknown DNS record data type."),
               };
    }

    private static string NormalizeDomainName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static string FormatErrors(IReadOnlyCollection<CloudflareApiError>? errors)
    {
        if (errors is null || errors.Count == 0)
        {
            return "Cloudflare returned an unsuccessful response.";
        }

        return "Cloudflare returned an unsuccessful response: "
               + string.Join(
                   "; ",
                   errors.Select(static error => error.Message ?? error.Code?.ToString(CultureInfo.InvariantCulture) ?? "unknown error"));
    }

    private sealed record CloudflareEnvelope<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("result")] T? Result,
        [property: JsonPropertyName("errors")] IReadOnlyCollection<CloudflareApiError>? Errors);

    private sealed record CloudflareApiError(
        [property: JsonPropertyName("code")] int? Code,
        [property: JsonPropertyName("message")] string? Message);

    private sealed record CloudflareZoneModel(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name);

    private sealed record CloudflareDnsRecordModel(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("content")] string? Content,
        [property: JsonPropertyName("ttl")] int Ttl,
        [property: JsonPropertyName("proxied")] bool? Proxied,
        [property: JsonPropertyName("comment")] string? Comment,
        [property: JsonPropertyName("priority")] int? Priority);

    private sealed record CloudflareDnsRecordRequest(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("ttl")] int Ttl,
        [property: JsonPropertyName("proxied")] bool Proxied,
        [property: JsonPropertyName("comment")] string? Comment,
        [property: JsonPropertyName("priority")] int? Priority);
}
#pragma warning restore MA0048
