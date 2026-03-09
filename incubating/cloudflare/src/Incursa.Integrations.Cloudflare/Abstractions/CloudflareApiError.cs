namespace Incursa.Integrations.Cloudflare.Abstractions;

public sealed record CloudflareApiError(int Code, string Message);
