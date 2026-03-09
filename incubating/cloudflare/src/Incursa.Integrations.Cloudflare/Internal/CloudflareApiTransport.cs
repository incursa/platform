using System.Net;
using System.Net.Http.Headers;
using Incursa.Integrations.Cloudflare.Abstractions;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Internal;

public sealed class CloudflareApiTransport
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, string, string, int, int, long, string?, Exception?> LogRawRetryMessage =
        LoggerMessage.Define<string, string, int, int, long, string?>(
            LogLevel.Warning,
            new EventId(1, nameof(LogRawRetryMessage)),
            "Cloudflare raw API retrying method={Method} path={Path} attempt={Attempt} status={StatusCode} elapsedMs={ElapsedMs} cfRay={CfRay}");

    private static readonly Action<ILogger, string, string, int, long, Exception?> LogRawTransportErrorMessage =
        LoggerMessage.Define<string, string, int, long>(
            LogLevel.Warning,
            new EventId(2, nameof(LogRawTransportErrorMessage)),
            "Cloudflare raw API transport error method={Method} path={Path} attempt={Attempt} elapsedMs={ElapsedMs}");

    private static readonly Action<ILogger, string, string, int, int, long, Exception?> LogRawTimeoutMessage =
        LoggerMessage.Define<string, string, int, int, long>(
            LogLevel.Warning,
            new EventId(6, nameof(LogRawTimeoutMessage)),
            "Cloudflare raw API timeout method={Method} path={Path} attempt={Attempt} timeoutSeconds={TimeoutSeconds} elapsedMs={ElapsedMs}");

    private static readonly Action<ILogger, string, string, int, int, long, string?, Exception?> LogRetryMessage =
        LoggerMessage.Define<string, string, int, int, long, string?>(
            LogLevel.Warning,
            new EventId(3, nameof(LogRetryMessage)),
            "Cloudflare API retrying method={Method} path={Path} attempt={Attempt} status={StatusCode} elapsedMs={ElapsedMs} cfRay={CfRay}");

    private static readonly Action<ILogger, string, string, int, long, string?, Exception?> LogApiDebugMessage =
        LoggerMessage.Define<string, string, int, long, string?>(
            LogLevel.Debug,
            new EventId(4, nameof(LogApiDebugMessage)),
            "Cloudflare API method={Method} path={Path} status={StatusCode} elapsedMs={ElapsedMs} cfRay={CfRay}");

    private static readonly Action<ILogger, string, string, int, long, Exception?> LogTransportErrorMessage =
        LoggerMessage.Define<string, string, int, long>(
            LogLevel.Warning,
            new EventId(5, nameof(LogTransportErrorMessage)),
            "Cloudflare API transport error method={Method} path={Path} attempt={Attempt} elapsedMs={ElapsedMs}");

    private static readonly Action<ILogger, string, string, int, int, long, Exception?> LogTimeoutMessage =
        LoggerMessage.Define<string, string, int, int, long>(
            LogLevel.Warning,
            new EventId(7, nameof(LogTimeoutMessage)),
            "Cloudflare API timeout method={Method} path={Path} attempt={Attempt} timeoutSeconds={TimeoutSeconds} elapsedMs={ElapsedMs}");

    private readonly HttpClient httpClient;
    private readonly CloudflareApiOptions options;
    private readonly ILogger<CloudflareApiTransport> logger;

    public CloudflareApiTransport(
        HttpClient httpClient,
        IOptions<CloudflareApiOptions> options,
        ILogger<CloudflareApiTransport> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (this.httpClient.BaseAddress is null)
        {
            this.httpClient.BaseAddress = new Uri($"{this.options.BaseUrl.AbsoluteUri.TrimEnd('/')}/", UriKind.Absolute);
        }

        this.httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, this.options.RequestTimeoutSeconds));

        if (!string.IsNullOrWhiteSpace(this.options.ApiToken) && this.httpClient.DefaultRequestHeaders.Authorization is null)
        {
            this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.options.ApiToken.Trim());
        }
    }

    public async Task<T> SendForResultAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var envelope = await SendForEnvelopeAsync<T>(method, path, body, cancellationToken).ConfigureAwait(false);
        if (!envelope.Success)
        {
            ThrowApiException(envelope, HttpStatusCode.BadRequest, null);
        }

        if (envelope.Result is null)
        {
            throw new CloudflareApiException($"Cloudflare response did not include a result payload for '{path}'.");
        }

        return envelope.Result;
    }

    public async Task<CloudflareRawResponse> SendForRawAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var retries = Math.Max(0, options.RetryCount);
        var timeoutSeconds = Math.Max(1, options.RequestTimeoutSeconds);
        for (var attempt = 0; attempt <= retries; attempt++)
        {
            using var request = BuildRequest(method, path, body);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, operationCts.Token).ConfigureAwait(false);
                sw.Stop();
                var cfRay = TryGetCfRay(response);
                var payloadText = await response.Content.ReadAsStringAsync(operationCts.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode && attempt < retries && IsRetryable(response.StatusCode))
                {
                    LogRawRetryMessage(logger, method.Method, path, attempt + 1, (int)response.StatusCode, sw.ElapsedMilliseconds, cfRay, null);
                    await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return new CloudflareRawResponse(response.StatusCode, payloadText, cfRay);
            }
            catch (OperationCanceledException ex) when (IsTransportTimeout(cancellationToken, timeoutCts))
            {
                sw.Stop();
                LogRawTimeoutMessage(logger, method.Method, path, attempt + 1, timeoutSeconds, sw.ElapsedMilliseconds, ex);

                if (attempt >= retries)
                {
                    throw CreateTimeoutException(path, timeoutSeconds, ex);
                }

                await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or OperationCanceledException)
            {
                sw.Stop();
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                LogRawTransportErrorMessage(logger, method.Method, path, attempt + 1, sw.ElapsedMilliseconds, ex);

                if (attempt >= retries)
                {
                    throw new CloudflareApiException($"Cloudflare API request failed for '{path}'.", innerException: ex);
                }

                await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new CloudflareApiException($"Cloudflare API request failed for '{path}'.");
    }

    internal async Task<CloudflareApiEnvelope<T>> SendForEnvelopeAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var retries = Math.Max(0, options.RetryCount);
        var timeoutSeconds = Math.Max(1, options.RequestTimeoutSeconds);
        CloudflareApiException? lastException = null;

        for (var attempt = 0; attempt <= retries; attempt++)
        {
            using var request = BuildRequest(method, path, body);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, operationCts.Token).ConfigureAwait(false);
                sw.Stop();

                var cfRay = TryGetCfRay(response);
                var payloadText = await response.Content.ReadAsStringAsync(operationCts.Token).ConfigureAwait(false);
                var envelope = DeserializeEnvelope<T>(payloadText);

                if (!response.IsSuccessStatusCode || !envelope.Success)
                {
                    var exception = CreateApiException(path, response.StatusCode, envelope, cfRay, payloadText);
                    if (attempt < retries && IsRetryable(response.StatusCode))
                    {
                        LogRetryMessage(logger, method.Method, path, attempt + 1, (int)response.StatusCode, sw.ElapsedMilliseconds, cfRay, null);
                        await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
                        lastException = exception;
                        continue;
                    }

                    throw exception;
                }

                LogApiDebugMessage(logger, method.Method, path, (int)response.StatusCode, sw.ElapsedMilliseconds, cfRay, null);

                return envelope;
            }
            catch (CloudflareApiException ex)
            {
                lastException = ex;
                if (attempt >= retries)
                {
                    throw;
                }

                await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (IsTransportTimeout(cancellationToken, timeoutCts))
            {
                sw.Stop();
                LogTimeoutMessage(logger, method.Method, path, attempt + 1, timeoutSeconds, sw.ElapsedMilliseconds, ex);

                if (attempt >= retries)
                {
                    throw CreateTimeoutException(path, timeoutSeconds, ex);
                }

                await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or OperationCanceledException)
            {
                sw.Stop();
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                LogTransportErrorMessage(logger, method.Method, path, attempt + 1, sw.ElapsedMilliseconds, ex);

                if (attempt >= retries)
                {
                    throw new CloudflareApiException($"Cloudflare API request failed for '{path}'.", innerException: ex);
                }

                await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new CloudflareApiException($"Cloudflare API request failed for '{path}'.");
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string path, object? body)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        HttpRequestMessage request = new HttpRequestMessage(method, path.TrimStart('/'));
        if (body is not null)
        {
            if (body is string textPayload)
            {
                request.Content = new StringContent(textPayload, Encoding.UTF8, "text/plain");
            }
            else
            {
                var payload = JsonSerializer.Serialize(body, Json);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }
        }

        return request;
    }

    private static CloudflareApiEnvelope<T> DeserializeEnvelope<T>(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new CloudflareApiEnvelope<T>(false, default, Array.Empty<CloudflareApiError>(), Array.Empty<string>(), null);
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<CloudflareApiEnvelope<T>>(payload, Json);
            return envelope ?? new CloudflareApiEnvelope<T>(false, default, Array.Empty<CloudflareApiError>(), Array.Empty<string>(), null);
        }
        catch (JsonException)
        {
            return new CloudflareApiEnvelope<T>(false, default, Array.Empty<CloudflareApiError>(), Array.Empty<string>(), null);
        }
    }

    private static CloudflareApiException CreateApiException<T>(string path, HttpStatusCode statusCode, CloudflareApiEnvelope<T> envelope, string? cfRay, string payload)
    {
        var message = envelope.ErrorItems.Count > 0
            ? string.Join("; ", envelope.ErrorItems.Select(static e => $"{e.Code}: {e.Message}"))
            : $"Cloudflare API request failed for '{path}'. status={(int)statusCode}.";

        return new CloudflareApiException($"{message} payload={payload}", statusCode, envelope.ErrorItems, cfRay);
    }

    private static bool IsRetryable(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return status == 429 || status >= 500;
    }

    private static bool IsTransportTimeout(CancellationToken callerCancellationToken, CancellationTokenSource timeoutCts)
        => timeoutCts.IsCancellationRequested && !callerCancellationToken.IsCancellationRequested;

    private static CloudflareApiException CreateTimeoutException(string path, int timeoutSeconds, Exception innerException)
        => new($"Cloudflare API request timed out for '{path}' after {timeoutSeconds} second(s).", innerException);

    private static TimeSpan ComputeDelay(int attempt)
        => TimeSpan.FromMilliseconds(Math.Min(2000, 200 * (attempt + 1)));

    private static string? TryGetCfRay(HttpResponseMessage response)
        => response.Headers.TryGetValues("cf-ray", out var values) ? values.FirstOrDefault() : null;

    private static void ThrowApiException<T>(CloudflareApiEnvelope<T> envelope, HttpStatusCode statusCode, string? cfRay)
    {
        var message = envelope.ErrorItems.Count > 0
            ? string.Join("; ", envelope.ErrorItems.Select(static e => $"{e.Code}: {e.Message}"))
            : "Cloudflare request failed.";

        throw new CloudflareApiException(message, statusCode, envelope.ErrorItems, cfRay);
    }

    internal sealed record CloudflareApiEnvelope<T>(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("result")] T? Result,
        [property: JsonPropertyName("errors")] IReadOnlyList<CloudflareApiError>? Errors,
        [property: JsonPropertyName("messages")] IReadOnlyList<string>? Messages,
        [property: JsonPropertyName("result_info")] JsonElement? ResultInfo)
    {
        public IReadOnlyList<CloudflareApiError> ErrorItems { get; } = Errors ?? Array.Empty<CloudflareApiError>();

        public IReadOnlyList<string> MessageItems { get; } = Messages ?? Array.Empty<string>();
    }
}
