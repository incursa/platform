namespace Incursa.Integrations.ElectronicNotary.Proof;

internal sealed class NoOpProofTelemetry : IProofTelemetry
{
    public void TrackRetry(HttpMethod method, string route, int attempt, int? statusCode, string? errorType)
    {
    }

    public void TrackCircuitOpened(DateTimeOffset openedUntilUtc, string reason)
    {
    }

    public void TrackCircuitRejected(HttpMethod method, string route, DateTimeOffset openedUntilUtc)
    {
    }
}
