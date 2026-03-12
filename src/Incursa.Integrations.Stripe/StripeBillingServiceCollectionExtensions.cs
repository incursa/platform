using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Stripe;

public static class StripeBillingServiceCollectionExtensions
{
    public static IServiceCollection AddStripeBilling(
        this IServiceCollection services,
        Action<StripeBillingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<StripeBillingOptions>();
        services.Configure(configure);
        services.TryAddSingleton(static sp => sp.GetRequiredService<IOptions<StripeBillingOptions>>().Value);
        services.AddHttpClient<IStripeBillingClient, StripeBillingClient>();
        return services;
    }
}
