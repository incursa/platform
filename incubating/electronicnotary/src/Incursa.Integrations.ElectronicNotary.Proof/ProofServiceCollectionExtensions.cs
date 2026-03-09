namespace Incursa.Integrations.ElectronicNotary.Proof;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
/// Service registration extensions for the Proof API client.
/// </summary>
public static class ProofServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IProofClient"/> and configures its HTTP behavior.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">The delegate used to configure <see cref="ProofClientOptions"/>.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddProofClient(this IServiceCollection services, Action<ProofClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<ProofClientOptions>().Configure(configure);
        services.TryAddSingleton<IProofTransactionRegistrationSink, NoOpProofTransactionRegistrationSink>();
        services.TryAddSingleton<IProofTelemetry, NoOpProofTelemetry>();

        services
            .AddHttpClient<IProofClient, ProofClient>((serviceProvider, httpClient) =>
            {
                ProofClientOptions options = serviceProvider.GetRequiredService<IOptions<ProofClientOptions>>().Value;

                ValidateOptions(options);

                Uri resolvedBaseUrl = ResolveBaseUrl(options);
                httpClient.BaseAddress = resolvedBaseUrl;
                httpClient.Timeout = options.Timeout;

                httpClient.DefaultRequestHeaders.Remove("ApiKey");
                httpClient.DefaultRequestHeaders.Add("ApiKey", options.ApiKey);
            });

        return services;
    }

    private static void ValidateOptions(ProofClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Proof ApiKey must be configured.");
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proof timeout must be greater than zero.");
        }

        if (!options.EnableResilience)
        {
            return;
        }

        if (options.MaxRetryAttempts < 0)
        {
            throw new InvalidOperationException("Proof MaxRetryAttempts cannot be negative.");
        }

        if (options.InitialBackoff <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proof InitialBackoff must be greater than zero.");
        }

        if (options.MaxBackoff < options.InitialBackoff)
        {
            throw new InvalidOperationException("Proof MaxBackoff must be greater than or equal to InitialBackoff.");
        }

        if (options.CircuitBreakerFailureThreshold <= 0)
        {
            throw new InvalidOperationException("Proof CircuitBreakerFailureThreshold must be greater than zero.");
        }

        if (options.CircuitBreakDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proof CircuitBreakDuration must be greater than zero.");
        }
    }

    private static Uri ResolveBaseUrl(ProofClientOptions options)
    {
        if (options.BaseUrl is not null)
        {
            if (!options.BaseUrl.IsAbsoluteUri)
            {
                throw new InvalidOperationException("Proof BaseUrl must be an absolute URI.");
            }

            return EnsureTrailingSlash(options.BaseUrl);
        }

        Uri environmentUri = options.Environment switch
        {
            ProofEnvironment.Fairfax => ProofClientOptions.FairfaxBaseUrl,
            ProofEnvironment.Production => ProofClientOptions.ProductionBaseUrl,
            _ => throw new InvalidOperationException($"Unsupported Proof environment '{options.Environment}'."),
        };

        return EnsureTrailingSlash(environmentUri);
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        string value = uri.AbsoluteUri;
        if (value.EndsWith('/'))
        {
            return uri;
        }

        return new Uri($"{value}/", UriKind.Absolute);
    }
}
