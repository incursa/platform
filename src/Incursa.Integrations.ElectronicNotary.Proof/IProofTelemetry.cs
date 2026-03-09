namespace Incursa.Integrations.ElectronicNotary.Proof;

/// <summary>
/// Emits observability events for outbound Proof API calls and resilience behavior.
/// </summary>
public interface IProofTelemetry
{
    /// <summary>
    /// Records an outbound retry attempt.
    /// </summary>
    /// <param name="method">The HTTP method being retried.</param>
    /// <param name="route">The normalized API route.</param>
    /// <param name="attempt">The 1-based retry attempt number.</param>
    /// <param name="statusCode">The transient HTTP status code when available.</param>
    /// <param name="errorType">The transient exception type when available.</param>
    void TrackRetry(HttpMethod method, string route, int attempt, int? statusCode, string? errorType);

    /// <summary>
    /// Records circuit-breaker open events.
    /// </summary>
    /// <param name="openedUntilUtc">The UTC timestamp until which the circuit remains open.</param>
    /// <param name="reason">A short reason string that describes why the circuit opened.</param>
    void TrackCircuitOpened(DateTimeOffset openedUntilUtc, string reason);

    /// <summary>
    /// Records requests rejected because the circuit is currently open.
    /// </summary>
    /// <param name="method">The rejected HTTP method.</param>
    /// <param name="route">The rejected normalized API route.</param>
    /// <param name="openedUntilUtc">The UTC timestamp when the circuit closes.</param>
    void TrackCircuitRejected(HttpMethod method, string route, DateTimeOffset openedUntilUtc);
}
