namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Bravellian.Platform.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Endpoint mapping extensions for Proof webhooks.
/// </summary>
public static class ProofWebhookEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Proof webhook endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern for Proof webhooks.</param>
    /// <returns>The configured route handler builder.</returns>
    public static RouteHandlerBuilder MapProofWebhooks(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/webhooks/proof")
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost(
            pattern,
            static async (HttpContext context, IWebhookIngestor ingestor, CancellationToken cancellationToken) =>
                await ProofWebhookEndpoint.HandleAsync(context, ingestor, cancellationToken).ConfigureAwait(false));
    }
}
