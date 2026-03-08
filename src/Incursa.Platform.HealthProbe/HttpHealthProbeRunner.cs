using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using Incursa.Platform.Health;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Executes health probes over HTTP against standardized platform health endpoints.
/// </summary>
public sealed class HttpHealthProbeRunner : IHealthProbeRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal const string HttpClientName = "Incursa.Platform.HealthProbe";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<HttpHealthProbeRunner> logger;
    private readonly IOptions<HealthProbeOptions> options;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHealthProbeRunner"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create HTTP clients.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Configured health probe options.</param>
    public HttpHealthProbeRunner(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpHealthProbeRunner> logger,
        IOptions<HealthProbeOptions> options)
    {
        this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Operational probe failures map to non-healthy results.")]
    public async Task<HealthProbeResult> RunAsync(HealthProbeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startedAt = TimeProvider.System.GetTimestamp();
        var httpOptions = options.Value.Http ?? new HealthProbeHttpOptions();
        var target = ResolveTargetUri(httpOptions, request.Bucket);

        try
        {
            using var client = CreateClient(httpOptions);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, target);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(httpOptions.ApiKey))
            {
                if (string.IsNullOrWhiteSpace(httpOptions.ApiKeyHeaderName))
                {
                    throw new HealthProbeArgumentException("HTTP API key header name is required when API key is configured.");
                }

                if (!httpRequest.Headers.TryAddWithoutValidation(httpOptions.ApiKeyHeaderName, httpOptions.ApiKey))
                {
                    throw new HealthProbeArgumentException($"Invalid HTTP API key header name '{httpOptions.ApiKeyHeaderName}'.");
                }
            }

            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var responseBody = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (TryDeserializePayload(responseBody, out var payload))
            {
                var normalized = NormalizePayload(payload, request.Bucket);
                if (!response.IsSuccessStatusCode && IsHealthy(normalized.Status))
                {
                    normalized = normalized with
                    {
                        Status = "Unhealthy",
                    };
                }

                var exitCode = IsHealthy(normalized.Status)
                    ? HealthProbeExitCodes.Healthy
                    : HealthProbeExitCodes.NonHealthy;
                return new HealthProbeResult(
                    normalized.Bucket,
                    normalized.Status,
                    exitCode,
                    normalized,
                    TimeProvider.System.GetElapsedTime(startedAt));
            }

            var failureMessage = response.IsSuccessStatusCode
                ? "HTTP probe response payload was not a valid platform health payload."
                : $"HTTP probe returned {(int)response.StatusCode} ({response.ReasonPhrase}).";

            return CreateFailureResult(
                request.Bucket,
                failureMessage,
                TimeProvider.System.GetElapsedTime(startedAt));
        }
        catch (HealthProbeArgumentException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HTTP health probe failed for bucket {Bucket}.", request.Bucket);
            return CreateFailureResult(
                request.Bucket,
                $"HTTP probe failed: {ex.Message}",
                TimeProvider.System.GetElapsedTime(startedAt));
        }
    }

    private static Uri ResolveTargetUri(HealthProbeHttpOptions options, string bucket)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);

        var rawPath = ResolveBucketPath(options, bucket);
        if (Uri.TryCreate(rawPath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        if (options.BaseUrl is null)
        {
            throw new HealthProbeArgumentException("HTTP probe base URL is required for relative endpoint paths.");
        }

        var normalizedPath = rawPath.StartsWith("/", StringComparison.Ordinal) ? rawPath : $"/{rawPath}";
        return new Uri(options.BaseUrl, normalizedPath);
    }

    private static string ResolveBucketPath(HealthProbeHttpOptions options, string bucket)
    {
        if (bucket.Equals(PlatformHealthTags.Live, StringComparison.OrdinalIgnoreCase))
        {
            return options.LivePath;
        }

        if (bucket.Equals(PlatformHealthTags.Ready, StringComparison.OrdinalIgnoreCase))
        {
            return options.ReadyPath;
        }

        if (bucket.Equals(PlatformHealthTags.Dep, StringComparison.OrdinalIgnoreCase))
        {
            return options.DepPath;
        }

        throw new HealthProbeArgumentException($"Unknown bucket '{bucket}'.");
    }

    private HttpClient CreateClient(HealthProbeHttpOptions options)
    {
        if (!options.AllowInsecureTls)
        {
            return httpClientFactory.CreateClient(HttpClientName);
        }

        return CreateInsecureClient();
    }

    [SuppressMessage("Security", "CA5359:Do not disable certificate validation", Justification = "Explicit operator opt-in for deployment diagnostics.")]
    private static HttpClient CreateInsecureClient()
    {
        var handler = new HttpClientHandler
        {
#pragma warning disable MA0039 // Explicitly opt-in via configuration for diagnostics and legacy environments.
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
#pragma warning restore MA0039
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
    }

    private static bool TryDeserializePayload(string? body, [NotNullWhen(true)] out PlatformHealthReportPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            payload = JsonSerializer.Deserialize<PlatformHealthReportPayload>(body, JsonOptions);
            return payload is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static PlatformHealthReportPayload NormalizePayload(PlatformHealthReportPayload payload, string fallbackBucket)
    {
        var bucket = string.IsNullOrWhiteSpace(payload.Bucket) ? fallbackBucket : payload.Bucket;
        var status = string.IsNullOrWhiteSpace(payload.Status) ? "Unhealthy" : payload.Status;
        var checks = payload.Checks ?? Array.Empty<PlatformHealthCheckEntry>();

        return new PlatformHealthReportPayload(
            bucket,
            status,
            payload.TotalDurationMs,
            checks);
    }

    private static bool IsHealthy(string status)
    {
        return string.Equals(status, "Healthy", StringComparison.OrdinalIgnoreCase);
    }

    private static HealthProbeResult CreateFailureResult(string bucket, string description, TimeSpan duration)
    {
        var payload = new PlatformHealthReportPayload(
            bucket,
            "Unhealthy",
            0,
            new[]
            {
                new PlatformHealthCheckEntry(
                    "http_probe",
                    "Unhealthy",
                    0,
                    description,
                    null),
            });

        return new HealthProbeResult(
            bucket,
            payload.Status,
            HealthProbeExitCodes.NonHealthy,
            payload,
            duration);
    }
}
