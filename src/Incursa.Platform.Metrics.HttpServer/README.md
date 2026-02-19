# Incursa.Platform.Metrics.HttpServer

Self-hosted Prometheus metrics server for Incursa.Platform using the OpenTelemetry HTTP listener exporter.

## Install

```bash
dotnet add package Incursa.Platform.Metrics.HttpServer
```

## Usage

```csharp
using Incursa.Platform.Metrics.HttpServer;

using var server = new PlatformMetricsHttpServer(new PlatformMetricsHttpServerOptions
{
    Meter = new PlatformMeterOptions
    {
        MeterName = "Incursa.Platform.MyApp"
    },
    UriPrefixes = ["http://localhost:9464/"],
    ScrapeEndpointPath = "/metrics"
});

Console.WriteLine("Prometheus scrape endpoint running at http://localhost:9464/metrics");
Console.ReadLine();
```
