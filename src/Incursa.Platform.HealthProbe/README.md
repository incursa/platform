# Incursa.Platform.HealthProbe

Runs health checks with standardized exit codes in either:
- `inprocess` mode: resolve and execute checks from the host DI container.
- `http` mode: call the running app's health endpoints (`/healthz`, `/readyz`, `/depz` by default).

## Command

```text
health [live|ready|dep]
health list
```

Options:

- `--timeout <seconds>`: timeout for the whole execution (default 2s)
- `--mode <inprocess|http>`: override configured mode for this invocation
- `--json`: emit JSON payload
- `--include-data`: include filtered check `data` entries in output

Default bucket: `ready`
Default mode: `inprocess`

Exit codes:

- `0`: healthy
- `1`: non-healthy
- `2`: misconfiguration/exception

## Configuration

```json
{
  "Incursa": {
    "HealthProbe": {
      "Mode": "http",
      "DefaultBucket": "ready",
      "TimeoutSeconds": 2,
      "Http": {
        "BaseUrl": "http://127.0.0.1:8080",
        "LivePath": "/healthz",
        "ReadyPath": "/readyz",
        "DepPath": "/depz",
        "ApiKey": "replace-me",
        "ApiKeyHeaderName": "X-Api-Key",
        "AllowInsecureTls": false
      }
    }
  }
}
```

## Caller Pattern (No Port Binding In `health` Mode)

Use a dual-builder startup so `health` invocations never start web hosting:

```csharp
using Incursa.Platform.HealthProbe;

if (HealthProbeApp.IsHealthCheckInvocation(args))
{
    var hostBuilder = Host.CreateApplicationBuilder(args);
    ConfigureSharedServices(hostBuilder);
    hostBuilder.UseIncursaHealthProbe();

    using var host = hostBuilder.Build();
    return await HealthProbeApp.RunHealthCheckAsync(args, host.Services, CancellationToken.None);
}

var webBuilder = WebApplication.CreateBuilder(args);
ConfigureSharedServices(webBuilder);
webBuilder.UseIncursaHealthProbe();

var app = webBuilder.Build();
app.MapPlatformHealthEndpoints();
await app.RunAsync();

static void ConfigureSharedServices(IHostApplicationBuilder builder)
{
    builder.Services.AddPlatformHealthChecks();
    // register application services used by your health checks
}
```
