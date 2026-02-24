namespace Incursa.Platform.Health;

public sealed record PlatformHealthReportPayload(
    string Bucket,
    string Status,
    double TotalDurationMs,
    IReadOnlyList<PlatformHealthCheckEntry> Checks);
