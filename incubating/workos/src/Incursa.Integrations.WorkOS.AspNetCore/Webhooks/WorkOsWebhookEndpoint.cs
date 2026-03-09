namespace Incursa.Integrations.WorkOS.AspNetCore.Webhooks;

using Incursa.Integrations.WorkOS.Abstractions.Webhooks;
using Microsoft.AspNetCore.Http;

public sealed class WorkOsWebhookEndpoint : IMiddleware
{
    private readonly IWorkOsWebhookVerifier _verifier;
    private readonly IWorkOsWebhookProcessor _processor;

    public WorkOsWebhookEndpoint(IWorkOsWebhookVerifier verifier, IWorkOsWebhookProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        ArgumentNullException.ThrowIfNull(processor);
        _verifier = verifier;
        _processor = processor;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!context.Request.Path.StartsWithSegments("/workos/webhooks", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        using var buffer = new MemoryStream();
        await context.Request.Body.CopyToAsync(buffer, context.RequestAborted).ConfigureAwait(false);
        var bodyBytes = buffer.ToArray();

        var headers = context.Request.Headers.ToDictionary(static x => x.Key, static x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var verification = _verifier.Verify(headers, bodyBytes);
        if (!verification.IsValid)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = verification.FailureReason }, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        using var payload = JsonDocument.Parse(bodyBytes);
        var evt = BuildEvent(payload);
        var result = await _processor.ProcessAsync(evt, context.RequestAborted).ConfigureAwait(false);
        context.Response.StatusCode = result.Processed || result.Duplicate ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { processed = result.Processed, duplicate = result.Duplicate, message = result.Message }, context.RequestAborted).ConfigureAwait(false);
    }

    private static WorkOsWebhookEvent BuildEvent(JsonDocument payload)
    {
        var root = payload.RootElement;
        var eventId = TryGetString(root, "id") ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var eventType = TryGetString(root, "event") ?? TryGetString(root, "type") ?? "unknown";
        var orgId = TryGetString(root, "organization_id");
        var occurredUtc = TryGetDateTimeOffset(root, "created_at") ?? DateTimeOffset.UtcNow;
        return new WorkOsWebhookEvent(eventId, eventType, orgId, occurredUtc, payload);
    }

    private static string? TryGetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }
}

