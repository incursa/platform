namespace Incursa.Integrations.ElectronicNotary.Proof;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Microsoft.Extensions.Options;

internal sealed class ProofClient : IProofClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static readonly string[] CorrelationHeaderNames =
    [
        "X-Correlation-ID",
        "Correlation-ID",
        "X-Request-ID",
        "Request-ID",
        "Traceparent",
    ];

    private static readonly HashSet<string> IdempotentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethod.Get.Method,
        HttpMethod.Head.Method,
        HttpMethod.Put.Method,
        HttpMethod.Delete.Method,
        HttpMethod.Options.Method,
        HttpMethod.Trace.Method,
    };

    private readonly HttpClient httpClient;
    private readonly IProofTransactionRegistrationSink registrationSink;
    private readonly IOptionsMonitor<ProofClientOptions> optionsMonitor;
    private readonly IProofTelemetry telemetry;
    private readonly TimeProvider timeProvider;
    private readonly Lock resilienceStateGate = new Lock();
    private DateTimeOffset circuitOpenedUntilUtc = DateTimeOffset.MinValue;
    private int transientFailureCount;

    public ProofClient(
        HttpClient httpClient,
        IProofTransactionRegistrationSink registrationSink,
        IOptionsMonitor<ProofClientOptions> optionsMonitor,
        IProofTelemetry telemetry,
        TimeProvider? timeProvider = null)
    {
        this.httpClient = httpClient;
        this.registrationSink = registrationSink;
        this.optionsMonitor = optionsMonitor;
        this.telemetry = telemetry;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Transaction transaction = await this.SendForTransactionAsync(HttpMethod.Post, "transactions", request, cancellationToken).ConfigureAwait(false);
        await this.registrationSink.RegisterTransactionAsync(transaction.Id, cancellationToken).ConfigureAwait(false);
        return transaction;
    }

    public Task<Transaction> CreateDraftTransactionAsync(SignerInput signer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(signer);

        CreateTransactionRequest request = CreateTransactionRequest.Create([signer], null, true);
        return this.CreateTransactionAsync(request, cancellationToken);
    }

    public Task<Transaction> AddDocumentAsync(ProofTransactionId transactionId, AddDocumentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return this.SendForTransactionAsync(
            HttpMethod.Post,
            ProofClientHelpers.BuildTransactionRoute(transactionId, "documents"),
            request,
            cancellationToken);
    }

    public Task<Transaction> GetTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken = default)
    {
        return this.SendForTransactionAsync(
            HttpMethod.Get,
            ProofClientHelpers.BuildTransactionRoute(transactionId),
            null,
            cancellationToken);
    }

    private async Task<Transaction> SendForTransactionAsync(
        HttpMethod method,
        string route,
        object? payload,
        CancellationToken cancellationToken)
    {
        string normalizedRoute = ProofClientHelpers.NormalizeRoute(route);
        ProofClientOptions options = this.optionsMonitor.CurrentValue;
        bool canRetry = ProofResilienceHelpers.CanRetry(method, options);
        int attemptLimit = canRetry ? options.MaxRetryAttempts + 1 : 1;

        for (int attempt = 1; attempt <= attemptLimit; attempt++)
        {
            this.ThrowIfCircuitOpen(options, method, normalizedRoute);

            using var request = new HttpRequestMessage(method, normalizedRoute);
            if (payload is not null)
            {
                request.Content = JsonContent.Create(payload, options: SerializerOptions);
            }

            try
            {
                using HttpResponseMessage response = await this.httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    bool transientStatusCode = ProofResilienceHelpers.IsTransientStatusCode(response.StatusCode, options);
                    if (transientStatusCode)
                    {
                        this.RecordTransientFailure(options, $"status:{(int)response.StatusCode}");
                        if (attempt < attemptLimit)
                        {
                            this.telemetry.TrackRetry(method, normalizedRoute, attempt, (int)response.StatusCode, null);
                            await ProofResilienceHelpers.DelayBeforeRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                            continue;
                        }
                    }
                    else
                    {
                        this.RecordTransientSuccess();
                    }

                    throw await ProofClientHelpers.CreateApiExceptionAsync(
                        method,
                        route,
                        response,
                        CorrelationHeaderNames,
                        cancellationToken).ConfigureAwait(false);
                }

                this.RecordTransientSuccess();
                Transaction? transaction = await response.Content
                    .ReadFromJsonAsync<Transaction>(SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);

                return transaction ?? throw new InvalidOperationException($"Proof response payload was empty for route '{normalizedRoute}'.");
            }
            catch (HttpRequestException exception) when (canRetry && attempt < attemptLimit)
            {
                this.RecordTransientFailure(options, exception.GetType().Name);
                this.telemetry.TrackRetry(method, normalizedRoute, attempt, null, exception.GetType().Name);
                await ProofResilienceHelpers.DelayBeforeRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
                when (canRetry &&
                      !cancellationToken.IsCancellationRequested &&
                      attempt < attemptLimit)
            {
                this.RecordTransientFailure(options, exception.GetType().Name);
                this.telemetry.TrackRetry(method, normalizedRoute, attempt, (int)HttpStatusCode.RequestTimeout, exception.GetType().Name);
                await ProofResilienceHelpers.DelayBeforeRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception) when (canRetry)
            {
                this.RecordTransientFailure(options, exception.GetType().Name);
                throw;
            }
            catch (OperationCanceledException exception)
                when (canRetry && !cancellationToken.IsCancellationRequested)
            {
                this.RecordTransientFailure(options, exception.GetType().Name);
                throw;
            }
        }

        throw new InvalidOperationException($"Proof API request failed after exhausting retries for route '{normalizedRoute}'.");
    }

    private void ThrowIfCircuitOpen(ProofClientOptions options, HttpMethod method, string route)
    {
        if (!options.EnableResilience)
        {
            return;
        }

        DateTimeOffset nowUtc = this.timeProvider.GetUtcNow();
        DateTimeOffset openedUntilUtc;
        lock (this.resilienceStateGate)
        {
            openedUntilUtc = this.circuitOpenedUntilUtc;
        }

        if (openedUntilUtc <= nowUtc)
        {
            return;
        }

        this.telemetry.TrackCircuitRejected(method, route, openedUntilUtc);
        throw new HttpRequestException($"Proof API circuit breaker is open until '{openedUntilUtc:O}'.");
    }

    private void RecordTransientSuccess()
    {
        lock (this.resilienceStateGate)
        {
            this.transientFailureCount = 0;
            this.circuitOpenedUntilUtc = DateTimeOffset.MinValue;
        }
    }

    private void RecordTransientFailure(ProofClientOptions options, string reason)
    {
        if (!options.EnableResilience)
        {
            return;
        }

        DateTimeOffset nowUtc = this.timeProvider.GetUtcNow();
        DateTimeOffset? openedUntilUtc = null;

        lock (this.resilienceStateGate)
        {
            this.transientFailureCount++;
            if (this.transientFailureCount < options.CircuitBreakerFailureThreshold)
            {
                return;
            }

            this.transientFailureCount = 0;
            this.circuitOpenedUntilUtc = nowUtc.Add(options.CircuitBreakDuration);
            openedUntilUtc = this.circuitOpenedUntilUtc;
        }

        if (openedUntilUtc.HasValue)
        {
            this.telemetry.TrackCircuitOpened(openedUntilUtc.Value, reason);
        }
    }

    private static class ProofResilienceHelpers
    {
        public static bool CanRetry(HttpMethod method, ProofClientOptions options)
        {
            if (!options.EnableResilience || options.MaxRetryAttempts <= 0)
            {
                return false;
            }

            if (options.RetryUnsafeMethods)
            {
                return true;
            }

            return IdempotentMethods.Contains(method.Method);
        }

        public static bool IsTransientStatusCode(HttpStatusCode statusCode, ProofClientOptions options)
        {
            int statusCodeValue = (int)statusCode;
            return options.RetryOnStatusCodes.Any(candidate => candidate == statusCodeValue);
        }

        public static async Task DelayBeforeRetryAsync(int attempt, ProofClientOptions options, CancellationToken cancellationToken)
        {
            double exponent = Math.Pow(2d, Math.Max(0, attempt - 1));
            double backoffMs = options.InitialBackoff.TotalMilliseconds * exponent;
            backoffMs = Math.Min(backoffMs, options.MaxBackoff.TotalMilliseconds);

            // Keep jitter in a bounded range to avoid synchronized retry spikes.
            double jitterMultiplier = RandomNumberGenerator.GetInt32(800, 1201) / 1000d;
            backoffMs = Math.Max(1d, backoffMs * jitterMultiplier);
            await Task.Delay(TimeSpan.FromMilliseconds(backoffMs), cancellationToken).ConfigureAwait(false);
        }
    }

    private static class ProofClientHelpers
    {
        public static string BuildTransactionRoute(ProofTransactionId transactionId, string? suffix = null)
        {
            string transactionValue = transactionId.ToString();
            if (string.IsNullOrWhiteSpace(transactionValue))
            {
                throw new ArgumentException("Transaction ID cannot be empty.", nameof(transactionId));
            }

            string route = $"transactions/{transactionValue}";
            if (!string.IsNullOrWhiteSpace(suffix))
            {
                route = $"{route}/{suffix.Trim('/')}";
            }

            return route;
        }

        public static string NormalizeRoute(string route)
        {
            return route.Trim('/');
        }

        public static async Task<ProofApiException> CreateApiExceptionAsync(
            HttpMethod method,
            string route,
            HttpResponseMessage response,
            IEnumerable<string> correlationHeaderNames,
            CancellationToken cancellationToken)
        {
            string responseBody = await ReadResponseBodyAsync(response, cancellationToken).ConfigureAwait(false);
            string? correlationInfo = TryGetCorrelationInfo(response, correlationHeaderNames);
            string message = $"Proof API request failed with status {(int)response.StatusCode} ({response.StatusCode}) for {method.Method} {NormalizeRoute(route)}.";

            return new ProofApiException(message, response.StatusCode, responseBody, correlationInfo);
        }

        public static async Task<string> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.Content is null)
            {
                return string.Empty;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        public static string? TryGetCorrelationInfo(HttpResponseMessage response, IEnumerable<string> correlationHeaderNames)
        {
            foreach (string headerName in correlationHeaderNames)
            {
                if (response.Headers.TryGetValues(headerName, out IEnumerable<string>? headerValues))
                {
                    string? correlationValue = headerValues.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
                    if (!string.IsNullOrWhiteSpace(correlationValue))
                    {
                        return $"{headerName}={correlationValue}";
                    }
                }
            }

            return null;
        }
    }
}
