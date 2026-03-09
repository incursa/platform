namespace Incursa.Integrations.Cloudflare.Clients.Models;

public sealed record CloudflareCustomHostname(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("hostname")] string? Hostname,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("ssl")] CloudflareCustomHostnameSsl? Ssl,
    [property: JsonPropertyName("ownership_verification")] CloudflareOwnershipVerification? OwnershipVerification);

public sealed record CloudflareCustomHostnameSsl(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("method")] string? Method,
    [property: JsonPropertyName("validation_errors")] IReadOnlyList<CloudflareValidationError>? ValidationErrors);

public sealed record CloudflareValidationError(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("message_code")] string? MessageCode);

public sealed record CloudflareOwnershipVerification(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("value")] string? Value);

public sealed record CloudflareCustomHostnameListResult(
    [property: JsonPropertyName("result")] IReadOnlyList<CloudflareCustomHostname>? Result)
{
    public IReadOnlyList<CloudflareCustomHostname> Items { get; } = Result ?? Array.Empty<CloudflareCustomHostname>();
}

public sealed record CloudflareCreateCustomHostnameRequest(
    [property: JsonPropertyName("hostname")] string Hostname,
    [property: JsonPropertyName("ssl")] CloudflareCustomHostnameSslRequest Ssl);

public sealed record CloudflarePatchCustomHostnameRequest(
    [property: JsonPropertyName("ssl")] CloudflareCustomHostnameSslRequest? Ssl = null);

public sealed record CloudflareCustomHostnameSslRequest(
    [property: JsonPropertyName("method")] string Method = "http",
    [property: JsonPropertyName("type")] string Type = "dv");
