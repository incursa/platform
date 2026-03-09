#pragma warning disable MA0048
namespace Incursa.Platform.CustomDomains.Cloudflare;

using System.Net.Http.Headers;
using System.Text;
using Incursa.Platform.CustomDomains;
using Microsoft.Extensions.DependencyInjection;

public sealed class CloudflareCustomDomainsOptions
{
    public Uri BaseUrl { get; set; } = new("https://api.cloudflare.com/client/v4/", UriKind.Absolute);

    public string ApiToken { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string CertificateValidationMethod { get; set; } = "http";

    public string CertificateValidationType { get; set; } = "dv";
}

public interface ICloudflareCustomDomainSynchronizationService
{
    Task<CustomDomain> EnsureDomainAsync(string hostname, CancellationToken cancellationToken = default);

    Task<CustomDomain?> SyncByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    Task<CustomDomain?> SyncByHostnameAsync(string hostname, CancellationToken cancellationToken = default);
}

public static class CloudflareCustomDomainServiceCollectionExtensions
{
    public static IServiceCollection AddCloudflareCustomDomains(
        this IServiceCollection services,
        Action<CloudflareCustomDomainsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CloudflareCustomDomainsOptions();
        configure(options);
        ValidateOptions(options);

        services.AddSingleton(options);
        services.AddHttpClient<ICloudflareCustomDomainSynchronizationService, CloudflareCustomDomainSynchronizationService>((_, client) =>
        {
            client.BaseAddress = options.BaseUrl;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
        });

        return services;
    }

    private static void ValidateOptions(CloudflareCustomDomainsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ApiToken))
        {
            throw new InvalidOperationException("Cloudflare custom-domain options must define an API token.");
        }

        if (string.IsNullOrWhiteSpace(options.ZoneId))
        {
            throw new InvalidOperationException("Cloudflare custom-domain options must define a zone id.");
        }
    }
}

internal static class CloudflareCustomDomainDefaults
{
    public const string ProviderName = "cloudflare";
    public const string ResourceType = "custom-hostname";
}

internal sealed class CloudflareCustomDomainSynchronizationService : ICloudflareCustomDomainSynchronizationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly CloudflareCustomDomainsOptions options;
    private readonly ICustomDomainAdministrationService administration;
    private readonly ICustomDomainQueryService query;

    public CloudflareCustomDomainSynchronizationService(
        HttpClient httpClient,
        CloudflareCustomDomainsOptions options,
        ICustomDomainAdministrationService administration,
        ICustomDomainQueryService query)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.administration = administration ?? throw new ArgumentNullException(nameof(administration));
        this.query = query ?? throw new ArgumentNullException(nameof(query));
    }

    public async Task<CustomDomain> EnsureDomainAsync(string hostname, CancellationToken cancellationToken = default)
    {
        var normalizedHostname = NormalizeHostname(hostname);
        var existing = await GetByHostnameCoreAsync(normalizedHostname, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = await CreateCoreAsync(normalizedHostname, cancellationToken).ConfigureAwait(false);
        }

        return await UpsertLocalAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomDomain?> SyncByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "zones/" + Uri.EscapeDataString(options.ZoneId.Trim()) + "/custom_hostnames/" + Uri.EscapeDataString(externalId.Trim()));
        var result = await SendAsync<CloudflareCustomHostnameModel>(request, cancellationToken).ConfigureAwait(false);
        return result is null ? null : await UpsertLocalAsync(result, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomDomain?> SyncByHostnameAsync(string hostname, CancellationToken cancellationToken = default)
    {
        var normalizedHostname = NormalizeHostname(hostname);
        var result = await GetByHostnameCoreAsync(normalizedHostname, cancellationToken).ConfigureAwait(false);
        return result is null ? null : await UpsertLocalAsync(result, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CloudflareCustomHostnameModel?> GetByHostnameCoreAsync(
        string normalizedHostname,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "zones/" + Uri.EscapeDataString(options.ZoneId.Trim()) + "/custom_hostnames?hostname=" + Uri.EscapeDataString(normalizedHostname));
        var result = await SendAsync<List<CloudflareCustomHostnameModel>>(request, cancellationToken).ConfigureAwait(false);
        return result.FirstOrDefault();
    }

    private async Task<CloudflareCustomHostnameModel> CreateCoreAsync(
        string normalizedHostname,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(
            new CloudflareCreateCustomHostnameRequest(
                normalizedHostname,
                new CloudflareCustomHostnameSslRequest(
                    options.CertificateValidationMethod.Trim(),
                    options.CertificateValidationType.Trim())),
            SerializerOptions);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "zones/" + Uri.EscapeDataString(options.ZoneId.Trim()) + "/custom_hostnames")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        return await SendAsync<CloudflareCustomHostnameModel>(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CustomDomain> UpsertLocalAsync(
        CloudflareCustomHostnameModel model,
        CancellationToken cancellationToken)
    {
        var localId = await ResolveLocalIdAsync(model, cancellationToken).ConfigureAwait(false);
        var domain = MapDomain(localId, model);
        return await administration.UpsertDomainAsync(domain, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CustomDomainId> ResolveLocalIdAsync(
        CloudflareCustomHostnameModel model,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(model.Id))
        {
            var existingByExternalLink = await query.GetDomainByExternalLinkAsync(
                CloudflareCustomDomainDefaults.ProviderName,
                model.Id,
                CloudflareCustomDomainDefaults.ResourceType,
                cancellationToken).ConfigureAwait(false);
            if (existingByExternalLink is not null)
            {
                return existingByExternalLink.Id;
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Hostname))
        {
            var existingByHostname = await query.GetDomainByHostnameAsync(model.Hostname, cancellationToken).ConfigureAwait(false);
            if (existingByHostname is not null)
            {
                return existingByHostname.Id;
            }
        }

        var identifier = !string.IsNullOrWhiteSpace(model.Id)
            ? model.Id.Trim()
            : NormalizeHostname(model.Hostname ?? Guid.NewGuid().ToString("N"));
        return new CustomDomainId("cloudflare-custom-domain:" + Uri.EscapeDataString(identifier));
    }

    private static CustomDomain MapDomain(CustomDomainId localId, CloudflareCustomHostnameModel model)
    {
        var hostname = NormalizeHostname(model.Hostname ?? localId.Value);
        var externalLinks = string.IsNullOrWhiteSpace(model.Id)
            ? Array.Empty<CustomDomainExternalLink>()
            :
            [
                new CustomDomainExternalLink(
                    new CustomDomainExternalLinkId("cloudflare-custom-domain-link:" + Uri.EscapeDataString(model.Id)),
                    CloudflareCustomDomainDefaults.ProviderName,
                    model.Id,
                    CloudflareCustomDomainDefaults.ResourceType),
            ];

        return new CustomDomain(
            localId,
            hostname,
            MapLifecycleStatus(model.Status),
            MapCertificateStatus(model.Ssl?.Status),
            NormalizeNullable(model.Ssl?.Method),
            model.Ssl?.ValidationErrors is { Count: > 0 } errors ? NormalizeNullable(errors.First().Message) : null,
            MapOwnershipVerification(model.OwnershipVerification),
            DateTimeOffset.UtcNow,
            externalLinks);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<CloudflareEnvelope<T>>(payload, SerializerOptions)
            ?? throw new InvalidOperationException("Cloudflare returned an empty payload.");
        if (!envelope.Success)
        {
            throw new InvalidOperationException(FormatErrors(envelope.Errors));
        }

        return envelope.Result ?? throw new InvalidOperationException("Cloudflare returned no result payload.");
    }

    private static CustomDomainLifecycleStatus MapLifecycleStatus(string? status)
    {
        var normalized = NormalizeNullable(status)?.ToLowerInvariant();
        if (normalized is null)
        {
            return CustomDomainLifecycleStatus.Unknown;
        }

        return normalized switch
        {
            "active" => CustomDomainLifecycleStatus.Active,
            "deleted" => CustomDomainLifecycleStatus.Removed,
            "error" or "failed" or "rejected" or "blocked" => CustomDomainLifecycleStatus.Failed,
            _ when normalized.Contains("pending", StringComparison.Ordinal)
                   || normalized.Contains("initial", StringComparison.Ordinal)
                => CustomDomainLifecycleStatus.Pending,
            _ => CustomDomainLifecycleStatus.Unknown,
        };
    }

    private static CustomDomainCertificateStatus MapCertificateStatus(string? status)
    {
        var normalized = NormalizeNullable(status)?.ToLowerInvariant();
        if (normalized is null)
        {
            return CustomDomainCertificateStatus.Unknown;
        }

        return normalized switch
        {
            "active" => CustomDomainCertificateStatus.Active,
            "error" or "failed" or "rejected" => CustomDomainCertificateStatus.Failed,
            _ when normalized.Contains("pending", StringComparison.Ordinal)
                   || normalized.Contains("initial", StringComparison.Ordinal)
                => CustomDomainCertificateStatus.Pending,
            _ => CustomDomainCertificateStatus.Unknown,
        };
    }

    private static CustomDomainOwnershipVerification? MapOwnershipVerification(
        CloudflareOwnershipVerificationModel? ownershipVerification)
    {
        if (ownershipVerification is null
            || string.IsNullOrWhiteSpace(ownershipVerification.Name)
            || string.IsNullOrWhiteSpace(ownershipVerification.Value))
        {
            return null;
        }

        return new CustomDomainOwnershipVerification(
            NormalizeHostname(ownershipVerification.Name),
            ownershipVerification.Type?.Trim().ToLowerInvariant() switch
            {
                "txt" => CustomDomainVerificationRecordType.Txt,
                "cname" => CustomDomainVerificationRecordType.CName,
                "http" => CustomDomainVerificationRecordType.Http,
                _ => CustomDomainVerificationRecordType.Other,
            },
            ownershipVerification.Value.Trim());
    }

    private static string NormalizeHostname(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        return hostname.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatErrors(IReadOnlyCollection<CloudflareErrorModel>? errors)
    {
        if (errors is null || errors.Count == 0)
        {
            return "Cloudflare request failed.";
        }

        return string.Join("; ", errors.Select(static error => error.Message ?? "Cloudflare request failed."));
    }

    private sealed record CloudflareEnvelope<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("result")] T? Result,
        [property: JsonPropertyName("errors")] IReadOnlyCollection<CloudflareErrorModel>? Errors);

    private sealed record CloudflareErrorModel(
        [property: JsonPropertyName("message")] string? Message);

    private sealed record CloudflareCustomHostnameModel(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("hostname")] string? Hostname,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("ssl")] CloudflareCustomHostnameSslModel? Ssl,
        [property: JsonPropertyName("ownership_verification")] CloudflareOwnershipVerificationModel? OwnershipVerification);

    private sealed record CloudflareCustomHostnameSslModel(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("method")] string? Method,
        [property: JsonPropertyName("validation_errors")] IReadOnlyCollection<CloudflareValidationErrorModel>? ValidationErrors);

    private sealed record CloudflareValidationErrorModel(
        [property: JsonPropertyName("message")] string? Message);

    private sealed record CloudflareOwnershipVerificationModel(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("value")] string? Value);

    private sealed record CloudflareCreateCustomHostnameRequest(
        [property: JsonPropertyName("hostname")] string Hostname,
        [property: JsonPropertyName("ssl")] CloudflareCustomHostnameSslRequest Ssl);

    private sealed record CloudflareCustomHostnameSslRequest(
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("type")] string Type);
}
#pragma warning restore MA0048
