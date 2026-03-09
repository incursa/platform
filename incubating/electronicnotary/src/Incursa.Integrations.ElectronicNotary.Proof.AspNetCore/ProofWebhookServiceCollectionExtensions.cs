namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Bravellian.Platform.Webhooks;
using Bravellian.Platform.Webhooks.AspNetCore;
using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Service registration extensions for Proof webhook ingestion and dispatching.
/// </summary>
public static class ProofWebhookServiceCollectionExtensions
{
    /// <summary>
    /// Registers Proof webhook services and the Incursa webhook ingestion pipeline.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">Optional delegate used to configure <see cref="ProofWebhookOptions"/>.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddProofWebhooks(
        this IServiceCollection services,
        Action<ProofWebhookOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddOptions<ProofWebhookOptions>();
        services.AddSingleton<IProofWebhookSignatureVerifier, ProofWebhookSignatureVerifier>();
        services.AddSingleton<ProofWebhookAuthenticator>();
        services.AddSingleton<ProofWebhookClassifier>();
        services.AddSingleton<ProofWebhookDispatchHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IProofWebhookHandler, NoOpProofWebhookHandler>());
        services.TryAddSingleton<IProofHealingTransactionRegistry, NoOpProofHealingTransactionRegistry>();
        services.AddSingleton<IWebhookProvider, ProofWebhookProvider>();
        services.AddBravellianWebhooks();
        services.AddProofHealing();

        return services;
    }

    /// <summary>
    /// Registers background healing polling that periodically loads transactions from Proof.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">Optional delegate used to configure <see cref="ProofHealingOptions"/>.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddProofHealing(
        this IServiceCollection services,
        Action<ProofHealingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddOptions<ProofHealingOptions>();
        services.TryAddSingleton<IProofHealingTransactionSource, NoOpProofHealingTransactionSource>();
        services.AddSingleton<IProofTransactionRegistrationSink, ProofHealingTransactionRegistrationSink>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ProofHealingHostedService>());

        return services;
    }

    /// <summary>
    /// Registers durable healing persistence with DbUp migrations for SQL Server or PostgreSQL.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">The delegate used to configure <see cref="ProofHealingPersistenceOptions"/>.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddProofHealingPersistence(
        this IServiceCollection services,
        Action<ProofHealingPersistenceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<ProofHealingPersistenceOptions>().Configure(configure);
        services.AddSingleton<ProofHealingDbUpMigrator>();
        services.AddSingleton<ProofHealingDatabaseStore>();
        services.AddSingleton<IProofHealingTransactionSource>(static serviceProvider =>
            serviceProvider.GetRequiredService<ProofHealingDatabaseStore>());
        services.AddSingleton<IProofHealingTransactionRegistry>(static serviceProvider =>
            serviceProvider.GetRequiredService<ProofHealingDatabaseStore>());
        services.AddSingleton<IProofHealingObserver>(static serviceProvider =>
            serviceProvider.GetRequiredService<ProofHealingDatabaseStore>());
        services.AddSingleton<IProofTransactionRegistrationSink, ProofHealingTransactionRegistrationSink>();

        return services;
    }
}
