# Incursa.Platform.Metrics.AspNetCore

ASP.NET Core integration for Incursa.Platform metrics using OpenTelemetry and Prometheus.

## Install

```bash
dotnet add package Incursa.Platform.Metrics.AspNetCore
```

## Usage

```csharp
using Incursa.Platform.Metrics.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlatformMetrics(options =>
{
    options.EnablePrometheusExporter = true;
    options.PrometheusEndpointPath = "/metrics";
    options.Meter.MeterName = "Incursa.Platform.MyApp";
});

var app = builder.Build();

app.MapPlatformMetricsEndpoint();

app.Run();
```
