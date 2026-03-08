# Health Probe Dual Mode

`Incursa.Platform.HealthProbe` supports two execution modes:

- `inprocess` (default): run registered health checks from the current process service provider.
- `http`: call a running process over HTTP (`/healthz`, `/readyz`, `/depz` by default).

## Why this exists

For `exec`-style container probes, you can run the same executable with `health ...` while avoiding web server startup and port binding in the probe process.

## Startup pattern

Detect health invocation first and use a generic host for probe mode.

```csharp
using Incursa.Platform.HealthProbe;

return await MainAsync(args).ConfigureAwait(false);

static async Task<int> MainAsync(string[] args)
{
    if (HealthProbeApp.IsHealthCheckInvocation(args))
    {
        var hostBuilder = Host.CreateApplicationBuilder(args);
        ConfigureSharedServices(hostBuilder);
        hostBuilder.UseIncursaHealthProbe();

        using var host = hostBuilder.Build();
        return await HealthProbeApp.RunHealthCheckAsync(args, host.Services, CancellationToken.None).ConfigureAwait(false);
    }

    var webBuilder = WebApplication.CreateBuilder(args);
    ConfigureSharedServices(webBuilder);
    webBuilder.UseIncursaHealthProbe();

    var app = webBuilder.Build();
    app.MapPlatformHealthEndpoints();
    await app.RunAsync().ConfigureAwait(false);
    return 0;
}

static void ConfigureSharedServices(IHostApplicationBuilder builder)
{
    builder.Services.AddPlatformHealthChecks();
    // register all shared application services and options here
}
```

## Configuration

```json
{
  "Incursa": {
    "HealthProbe": {
      "Mode": "http",
      "TimeoutSeconds": 2,
      "Http": {
        "BaseUrl": "http://127.0.0.1:8080",
        "ApiKey": "replace-me",
        "ApiKeyHeaderName": "X-Api-Key"
      }
    }
  }
}
```

CLI can override mode per invocation:

```text
health ready --mode inprocess
health dep --mode http
```
