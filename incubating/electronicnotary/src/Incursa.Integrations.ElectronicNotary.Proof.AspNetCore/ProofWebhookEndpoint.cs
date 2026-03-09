namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Bravellian.Platform.Webhooks;
using Microsoft.AspNetCore.Http;

internal static class ProofWebhookEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        IWebhookIngestor ingestor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ingestor);

        WebhookEnvelope envelope = await CreateEnvelopeAsync(context, cancellationToken).ConfigureAwait(false);
        WebhookIngestResult result = await ingestor.IngestAsync(ProofWebhookOptions.ProviderName, envelope, cancellationToken).ConfigureAwait(false);

        if (result.Decision == WebhookIngestDecision.Rejected)
        {
            return Results.StatusCode((int)result.HttpStatusCode);
        }

        return Results.Ok();
    }

    private static async Task<WebhookEnvelope> CreateEnvelopeAsync(HttpContext context, CancellationToken cancellationToken)
    {
        HttpRequest request = context.Request;
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        byte[] bodyBytes;
        using (var memory = new MemoryStream())
        {
            await request.Body.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
            bodyBytes = memory.ToArray();
        }

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }

        return new WebhookEnvelope(
            ProofWebhookOptions.ProviderName,
            DateTimeOffset.UtcNow,
            request.Method,
            request.Path.HasValue ? request.Path.Value! : string.Empty,
            request.QueryString.HasValue ? request.QueryString.Value! : string.Empty,
            headers,
            request.ContentType,
            bodyBytes,
            context.Connection.RemoteIpAddress?.ToString());
    }
}
