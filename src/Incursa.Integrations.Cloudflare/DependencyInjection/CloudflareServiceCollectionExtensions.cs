using System.Net;
using System.Net.Sockets;
using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Incursa.Integrations.Cloudflare.Services;
using Incursa.Integrations.Cloudflare.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Incursa.Integrations.Cloudflare.DependencyInjection;

public static class CloudflareServiceCollectionExtensions
{
    private const string HttpClientName = "Incursa.Cloudflare.Api";

    public static IServiceCollection AddCloudflareIntegration(this IServiceCollection services, Action<CloudflareApiOptions>? configure = null)
    {
        services.AddOptions<CloudflareApiOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddHttpClient(HttpClientName, static (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudflareApiOptions>>().Value;
            client.BaseAddress = new Uri($"{options.BaseUrl.AbsoluteUri.TrimEnd('/')}/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds));
        }).ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudflareApiOptions>>().Value;
            if (!options.ForceIpv4)
            {
                return new HttpClientHandler();
            }

            return new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var endpoint = context.DnsEndPoint;
                    var addresses = await Dns.GetHostAddressesAsync(endpoint.Host, cancellationToken).ConfigureAwait(false);
                    var ipv4Addresses = addresses.Where(static address => address.AddressFamily == AddressFamily.InterNetwork).ToArray();
                    if (ipv4Addresses.Length == 0)
                    {
                        throw new HttpRequestException($"No IPv4 address resolved for host '{endpoint.Host}'.");
                    }

                    Exception? lastError = null;
                    foreach (var address in ipv4Addresses)
                    {
                        Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            await socket.ConnectAsync(address, endpoint.Port, cancellationToken).ConfigureAwait(false);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
                        {
                            lastError = ex;
                            socket.Dispose();
                            if (cancellationToken.IsCancellationRequested)
                            {
                                throw;
                            }
                        }
                    }

                    throw new HttpRequestException(
                        $"Unable to connect to '{endpoint.Host}:{endpoint.Port}' using IPv4.",
                        lastError);
                },
            };
        });

        services.TryAddSingleton<CloudflareApiTransport>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return ActivatorUtilities.CreateInstance<CloudflareApiTransport>(sp, factory.CreateClient(HttpClientName));
        });

        services.TryAddSingleton<ICloudflareKvClient, CloudflareKvClient>();
        services.TryAddSingleton<ICloudflareCustomHostnameClient, CloudflareCustomHostnameClient>();
        services.TryAddSingleton<ICloudflareLoadBalancerClient, CloudflareLoadBalancerClient>();
        services.TryAddSingleton<ICloudflareLoadBalancerPoolClient, CloudflareLoadBalancerPoolClient>();
        services.TryAddSingleton<ICloudflareLoadBalancerMonitorClient, CloudflareLoadBalancerMonitorClient>();

        services.TryAddSingleton<ICloudflareKvStore, CloudflareKvStore>();
        services.TryAddSingleton<ICloudflareDomainOnboardingService, CloudflareDomainOnboardingService>();
        services.TryAddSingleton<ICloudflareDomainSyncService>(sp => (ICloudflareDomainSyncService)sp.GetRequiredService<ICloudflareDomainOnboardingService>());

        services.TryAddSingleton<ICloudflareR2ClientFactory, CloudflareR2ClientFactory>();
        services.TryAddSingleton<ICloudflareR2BlobStore>(sp =>
        {
            var factory = sp.GetRequiredService<ICloudflareR2ClientFactory>();
            var r2Options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudflareR2Options>>().Value;
            if (string.IsNullOrWhiteSpace(r2Options.Bucket))
            {
                throw new InvalidOperationException($"Cloudflare R2 option '{nameof(CloudflareR2Options.Bucket)}' is required.");
            }

            return new CloudflareR2BlobStore(factory.CreateClient(), r2Options.Bucket.Trim());
        });

        return services;
    }

    public static IServiceCollection AddCloudflareIntegration(this IServiceCollection services, IConfiguration configuration, string sectionName = CloudflareApiOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<CloudflareApiOptions>(configuration.GetSection(sectionName));
        services.Configure<CloudflareR2Options>(configuration.GetSection(CloudflareR2Options.SectionName));
        services.Configure<CloudflareKvOptions>(configuration.GetSection(CloudflareKvOptions.SectionName));
        services.Configure<CloudflareCustomHostnameOptions>(configuration.GetSection(CloudflareCustomHostnameOptions.SectionName));
        services.Configure<CloudflareLoadBalancerOptions>(configuration.GetSection(CloudflareLoadBalancerOptions.SectionName));
        return services.AddCloudflareIntegration();
    }

    public static IServiceCollection AddCloudflareKv(this IServiceCollection services, Action<CloudflareKvOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services;
    }

    public static IServiceCollection AddCloudflareR2(this IServiceCollection services, Action<CloudflareR2Options> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services;
    }

    public static IServiceCollection AddCloudflareCustomHostnames(this IServiceCollection services, Action<CloudflareCustomHostnameOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services;
    }

    public static IServiceCollection AddCloudflareLoadBalancing(this IServiceCollection services, Action<CloudflareLoadBalancerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure(configure);
        return services;
    }

    /// <summary>
    /// Replaces Cloudflare storage services with deterministic in-memory implementations for tests.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCloudflareInMemoryStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<ICloudflareKvStore>();
        services.RemoveAll<ICloudflareR2BlobStore>();

        services.AddSingleton<ICloudflareKvStore, InMemoryCloudflareKvStore>();
        services.AddSingleton<ICloudflareR2BlobStore, InMemoryCloudflareR2BlobStore>();

        return services;
    }
}
