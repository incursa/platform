namespace Incursa.Integrations.WorkOS.Core.Clients;

using System.Net;
using System.Net.Http.Headers;
using Incursa.Integrations.WorkOS.Abstractions.Audit;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsAuditHttpClient : IWorkOsAuditClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;

    public WorkOsAuditHttpClient(HttpClient httpClient, WorkOsManagementOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.Timeout = options.RequestTimeout;
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }

        if (_httpClient.DefaultRequestHeaders.Accept.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public async ValueTask<WorkOsAuditCreateEventResult> CreateEventAsync(WorkOsAuditCreateEventRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OrganizationId))
        {
            throw new ArgumentException("OrganizationId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Action))
        {
            throw new ArgumentException("Action is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Actor.Id))
        {
            throw new ArgumentException("Actor.Id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Actor.Type))
        {
            throw new ArgumentException("Actor.Type is required.", nameof(request));
        }

        if (request.Targets.Count == 0)
        {
            throw new ArgumentException("At least one target is required.", nameof(request));
        }

        var payload = new
        {
            organization_id = request.OrganizationId,
            @event = new
            {
                action = request.Action,
                occurred_at = request.OccurredAtUtc,
                version = request.Version,
                actor = new
                {
                    id = request.Actor.Id,
                    type = request.Actor.Type,
                    name = request.Actor.Name,
                    metadata = request.Actor.Metadata,
                },
                targets = request.Targets.Select(static target => new
                {
                    id = target.Id,
                    type = target.Type,
                    name = target.Name,
                    metadata = target.Metadata,
                }).ToArray(),
                context = request.Context is null
                    ? null
                    : new
                    {
                        location = request.Context.Location,
                        user_agent = request.Context.UserAgent,
                    },
                metadata = request.Metadata,
            },
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "audit_logs/events")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, SerializerOptions), Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            message.Headers.TryAddWithoutValidation("Idempotency-Key", request.IdempotencyKey);
        }

        try
        {
            using var response = await _httpClient.SendAsync(message, ct).ConfigureAwait(false);
            var body = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw CreateFailure(response.StatusCode, body);
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return new WorkOsAuditCreateEventResult(string.Empty);
            }

            using var json = JsonDocument.Parse(body);
            var eventId = TryReadEventId(json.RootElement) ?? string.Empty;
            return new WorkOsAuditCreateEventResult(eventId);
        }
        catch (HttpRequestException ex)
        {
            throw new WorkOsAuditClientException("WorkOS audit request failed.", WorkOsAuditFailureKind.Transient, null, ex);
        }
    }

    private static WorkOsAuditClientException CreateFailure(HttpStatusCode statusCode, string? body)
    {
        var numericStatus = (int)statusCode;
        var failureKind = IsTransient(statusCode)
            ? WorkOsAuditFailureKind.Transient
            : WorkOsAuditFailureKind.Permanent;

        var message = $"WorkOS audit request failed with status code {numericStatus}.";
        if (!string.IsNullOrWhiteSpace(body))
        {
            message += " " + body;
        }

        return new WorkOsAuditClientException(message, failureKind, numericStatus);
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;
    }

    private static string? TryReadEventId(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (TryGetString(root, "id") is { } directId)
            {
                return directId;
            }

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                return TryGetString(data, "id");
            }
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
