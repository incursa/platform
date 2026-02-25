namespace Incursa.Platform.Audit.WorkOS.Internal;

using System.Net;
using System.Net.Http.Headers;
using System.Text;

internal sealed class HttpWorkOsAuditPublisher : IWorkOsAuditPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient httpClient;

    public HttpWorkOsAuditPublisher(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async ValueTask PublishAsync(string organizationId, WorkOsAuditOutboxEnvelope envelope, WorkOsAuditSinkOptions options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(options);

        httpClient.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        if (httpClient.DefaultRequestHeaders.Accept.Count == 0)
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        var payload = new
        {
            organization_id = organizationId,
            @event = new
            {
                action = envelope.Action,
                occurred_at = envelope.OccurredAtUtc,
                version = envelope.Version,
                actor = new
                {
                    id = envelope.ActorId,
                    type = envelope.ActorType,
                    name = envelope.ActorDisplay,
                },
                targets = envelope.Anchors.Select(static anchor => new
                {
                    id = anchor.AnchorId,
                    type = anchor.AnchorType,
                    metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["role"] = anchor.Role,
                    },
                }).ToArray(),
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "audit_logs/events")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json"),
        };

        if (options.UseEventIdAsIdempotencyKey)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", envelope.EventId);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw CreateFailure(response.StatusCode, body);
        }
        catch (WorkOsAuditPublishException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new WorkOsAuditPublishException("WorkOS audit request failed.", WorkOsAuditPublishFailureKind.Transient, null, ex);
        }
    }

    private static WorkOsAuditPublishException CreateFailure(HttpStatusCode statusCode, string? responseBody)
    {
        var kind = statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500
            ? WorkOsAuditPublishFailureKind.Transient
            : WorkOsAuditPublishFailureKind.Permanent;

        var statusCodeValue = (int)statusCode;
        var message = $"WorkOS audit request failed with status code {statusCodeValue}.";
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            message += " " + responseBody;
        }

        return new WorkOsAuditPublishException(message, kind, statusCodeValue);
    }
}
