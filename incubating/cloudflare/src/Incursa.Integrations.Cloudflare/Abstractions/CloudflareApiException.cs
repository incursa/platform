using System.Net;
using Incursa.Integrations.Cloudflare.Abstractions;

namespace Incursa.Integrations.Cloudflare.Abstractions;

public sealed class CloudflareApiException : InvalidOperationException
{
    public CloudflareApiException()
    {
    }

    public CloudflareApiException(string message)
        : base(message)
    {
    }

    public CloudflareApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public CloudflareApiException(
        string message,
        HttpStatusCode? statusCode = null,
        IReadOnlyList<CloudflareApiError>? errors = null,
        string? cfRay = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Errors = errors ?? Array.Empty<CloudflareApiError>();
        CfRay = cfRay;
    }

    public HttpStatusCode? StatusCode { get; } = null;

    public IReadOnlyList<CloudflareApiError> Errors { get; } = Array.Empty<CloudflareApiError>();

    public string? CfRay { get; } = null;
}
