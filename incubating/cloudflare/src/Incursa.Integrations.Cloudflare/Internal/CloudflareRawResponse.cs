using System.Net;

namespace Incursa.Integrations.Cloudflare.Internal;

public sealed record CloudflareRawResponse(HttpStatusCode StatusCode, string Body, string? CfRay);
