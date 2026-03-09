#pragma warning disable MA0048
namespace Incursa.Platform.CustomDomains.Tests;

using Incursa.Integrations.Cloudflare.CustomDomains;
using Incursa.Platform.CustomDomains.Internal;
using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

internal sealed class CustomDomainTestHarness : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    public CustomDomainTestHarness()
    {
        var services = new ServiceCollection();
        AddStorage(services, new InMemoryRecordStore<CustomDomain>());
        AddStorage(services, new InMemoryLookupStore<DomainByHostnameProjection>());
        AddStorage(services, new InMemoryLookupStore<DomainByExternalLinkProjection>());
        services.AddCustomDomains();

        serviceProvider = services.BuildServiceProvider();
    }

    public ICustomDomainAdministrationService Administration =>
        serviceProvider.GetRequiredService<ICustomDomainAdministrationService>();

    public ICustomDomainQueryService Query =>
        serviceProvider.GetRequiredService<ICustomDomainQueryService>();

    public static CustomDomain CreateDomain(
        string id,
        string hostname,
        CustomDomainLifecycleStatus lifecycleStatus = CustomDomainLifecycleStatus.Unknown,
        CustomDomainCertificateStatus certificateStatus = CustomDomainCertificateStatus.Unknown,
        string? certificateValidationMethod = null,
        string? lastError = null,
        CustomDomainOwnershipVerification? ownershipVerification = null,
        IReadOnlyCollection<CustomDomainExternalLink>? externalLinks = null) =>
        new(
            new CustomDomainId(id),
            hostname,
            lifecycleStatus,
            certificateStatus,
            certificateValidationMethod,
            lastError,
            ownershipVerification,
            lastSynchronizedUtc: new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero),
            externalLinks: externalLinks);

    public static CustomDomainExternalLink CreateCloudflareExternalLink(string externalId) =>
        new(
            new CustomDomainExternalLinkId("cloudflare-link/" + Uri.EscapeDataString(externalId)),
            "cloudflare",
            externalId,
            "custom-hostname");

    public static ICloudflareCustomDomainSynchronizationService CreateCloudflareSynchronizationService(
        HttpClient client,
        ICustomDomainAdministrationService administration,
        ICustomDomainQueryService query,
        Action<CloudflareCustomDomainsOptions>? configure = null)
    {
        var options = new CloudflareCustomDomainsOptions
        {
            ApiToken = "test-token",
            ZoneId = "zone-123",
            BaseUrl = new Uri("https://api.example.test/client/v4/", UriKind.Absolute),
        };
        configure?.Invoke(options);
        client.BaseAddress = options.BaseUrl;
        return new CloudflareCustomDomainSynchronizationService(client, options, administration, query);
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
