using Incursa.Integrations.Cloudflare.Abstractions;
using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.KvProbe;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            KvProbeOptions options = KvProbeOptions.FromArgs(args);
            if (options.ShowHelp)
            {
                WriteUsage();
                return 0;
            }

            var loaded = await options.LoadConfigAsync().ConfigureAwait(false);
            RunBanner(loaded);

            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri(loaded.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute),
            };

            CloudflareApiOptions apiOptions = new CloudflareApiOptions
            {
                BaseUrl = new Uri(loaded.ApiBaseUrl, UriKind.Absolute),
                ApiToken = loaded.ApiToken,
                AccountId = loaded.AccountId,
                RequestTimeoutSeconds = loaded.RequestTimeoutSeconds,
                RetryCount = loaded.RetryCount,
            };

            CloudflareKvOptions kvOptions = new CloudflareKvOptions
            {
                AccountId = loaded.AccountId,
                NamespaceId = loaded.NamespaceId,
            };

            CloudflareApiTransport transport = new CloudflareApiTransport(
                httpClient,
                new OptionsWrapper<CloudflareApiOptions>(apiOptions),
                NullLogger<CloudflareApiTransport>.Instance);
            CloudflareKvClient client = new CloudflareKvClient(
                transport,
                new OptionsWrapper<CloudflareApiOptions>(apiOptions),
                new OptionsWrapper<CloudflareKvOptions>(kvOptions));

            var probeKey = ResolveProbeKey(loaded);
            var probePrefix = ResolveProbePrefix(loaded, probeKey);
            var probeValue = loaded.Value ?? $"kvprobe-value-{DateTimeOffset.UtcNow:O}";

            Console.WriteLine($"[step 1/5] PUT key={probeKey}");
            await client.PutAsync(probeKey, probeValue, CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine("  success");

            Console.WriteLine($"[step 2/5] GET key={probeKey}");
            var fetched = await client.GetAsync(probeKey, CancellationToken.None).ConfigureAwait(false);
            if (!string.Equals(fetched, probeValue, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("  failed: fetched value did not match written value.");
                Console.Error.WriteLine($"  expected={probeValue}");
                Console.Error.WriteLine($"  actual={fetched ?? "<null>"}");
                return 2;
            }

            Console.WriteLine("  success");

            Console.WriteLine($"[step 3/5] LIST prefix={probePrefix}");
            List<string> keys = new List<string>();
            await foreach (var key in client.ListKeysAsync(probePrefix, CancellationToken.None).ConfigureAwait(false))
            {
                keys.Add(key);
                if (keys.Count >= 25)
                {
                    break;
                }
            }

            Console.WriteLine($"  listed={keys.Count}");
            if (keys.Count > 0)
            {
                Console.WriteLine($"  sample={string.Join(", ", keys.Take(5))}");
            }

            if (!loaded.KeepKey)
            {
                Console.WriteLine($"[step 4/5] DELETE key={probeKey}");
                await client.DeleteAsync(probeKey, CancellationToken.None).ConfigureAwait(false);
                Console.WriteLine("  success");

                Console.WriteLine($"[step 5/5] GET-after-delete key={probeKey}");
                var afterDelete = await client.GetAsync(probeKey, CancellationToken.None).ConfigureAwait(false);
                if (afterDelete is not null)
                {
                    Console.Error.WriteLine("  failed: key still returns a value after delete.");
                    return 3;
                }

                Console.WriteLine("  success");
            }
            else
            {
                Console.WriteLine("[step 4/5] DELETE skipped (--keep-key true)");
                Console.WriteLine("[step 5/5] GET-after-delete skipped (--keep-key true)");
            }

            Console.WriteLine();
            Console.WriteLine("KV probe completed successfully.");
            return 0;
        }
        catch (CloudflareApiException ex)
        {
            Console.Error.WriteLine("Cloudflare API error:");
            Console.Error.WriteLine($"  message={ex.Message}");
            Console.Error.WriteLine($"  status={(int?)ex.StatusCode ?? 0}");
            Console.Error.WriteLine($"  cf-ray={ex.CfRay ?? "<none>"}");
            if (ex.Errors.Count > 0)
            {
                foreach (var error in ex.Errors)
                {
                    Console.Error.WriteLine($"  error={error.Code}:{error.Message}");
                }
            }

            return 10;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled probe failure:");
            Console.Error.WriteLine(ex.ToString());
            return 11;
        }
    }

    private static string ResolveProbeKey(ResolvedKvProbeOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Key))
        {
            return options.Key.Trim();
        }

        var prefix = string.IsNullOrWhiteSpace(options.KeyPrefix) ? "kvprobe" : options.KeyPrefix.Trim().Trim('/');
        return $"{prefix}/{DateTimeOffset.UtcNow:yyyyMMddHHmmss}/{Guid.NewGuid():N}";
    }

    private static string ResolveProbePrefix(ResolvedKvProbeOptions options, string key)
    {
        if (!string.IsNullOrWhiteSpace(options.ListPrefix))
        {
            return options.ListPrefix.Trim();
        }

        if (!string.IsNullOrWhiteSpace(options.KeyPrefix))
        {
            return options.KeyPrefix.Trim();
        }

        var slash = key.LastIndexOf("/", StringComparison.Ordinal);
        return slash <= 0 ? key : key[..slash];
    }

    private static void RunBanner(ResolvedKvProbeOptions options)
    {
        Console.WriteLine("Cloudflare KV probe");
        Console.WriteLine($"  baseUrl={options.ApiBaseUrl}");
        Console.WriteLine($"  accountId={Mask(options.AccountId)}");
        Console.WriteLine($"  namespaceId={Mask(options.NamespaceId)}");
        Console.WriteLine($"  config={options.ConfigPath}");
        Console.WriteLine($"  keepKey={options.KeepKey}");
        Console.WriteLine();
    }

    private static string Mask(string value)
    {
        if (value.Length <= 8)
        {
            return value;
        }

        return value[..4] + "..." + value[^4..];
    }

    private static void WriteUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/Incursa.Integrations.Cloudflare.KvProbe/Incursa.Integrations.Cloudflare.KvProbe.csproj -- --config <path> [--keep-key]");
        Console.WriteLine();
        Console.WriteLine("Config file fields (JSON):");
        Console.WriteLine("  apiBaseUrl           optional, default https://api.cloudflare.com/client/v4");
        Console.WriteLine("  apiToken             required");
        Console.WriteLine("  accountId            required");
        Console.WriteLine("  namespaceId          required");
        Console.WriteLine("  key                  optional fixed key");
        Console.WriteLine("  keyPrefix            optional, used when key is omitted");
        Console.WriteLine("  listPrefix           optional, used for LIST step");
        Console.WriteLine("  value                optional value for PUT");
        Console.WriteLine("  keepKey              optional bool");
        Console.WriteLine("  requestTimeoutSeconds optional int, default 8");
        Console.WriteLine("  retryCount           optional int, default 2");
    }
}

internal sealed class KvProbeOptions
{
    public string? ConfigPath { get; private set; }

    public bool KeepKey { get; private set; }

    public bool ShowHelp { get; private set; }

    public static KvProbeOptions FromArgs(string[] args)
    {
        KvProbeOptions result = new KvProbeOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (string.Equals(token, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(token, "-h", StringComparison.OrdinalIgnoreCase))
            {
                result.ShowHelp = true;
                continue;
            }

            if (string.Equals(token, "--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                i++;
                result.ConfigPath = args[i];
                continue;
            }

            if (string.Equals(token, "--keep-key", StringComparison.OrdinalIgnoreCase))
            {
                result.KeepKey = true;
                continue;
            }
        }

        return result;
    }

    public async Task<ResolvedKvProbeOptions> LoadConfigAsync()
    {
        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            throw new InvalidOperationException("Missing required --config <path> argument.");
        }

        var fullPath = Path.GetFullPath(ConfigPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Config file was not found.", fullPath);
        }

        var json = await File.ReadAllTextAsync(fullPath, Encoding.UTF8).ConfigureAwait(false);
        var config = JsonSerializer.Deserialize<KvProbeConfig>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Unable to parse config file.");

        var apiToken = Required(config.ApiToken, "apiToken");
        var accountId = Required(config.AccountId, "accountId");
        var namespaceId = Required(config.NamespaceId, "namespaceId");

        return new ResolvedKvProbeOptions(
            ConfigPath: fullPath,
            ApiBaseUrl: string.IsNullOrWhiteSpace(config.ApiBaseUrl) ? "https://api.cloudflare.com/client/v4" : config.ApiBaseUrl.Trim(),
            ApiToken: apiToken,
            AccountId: accountId,
            NamespaceId: namespaceId,
            Key: config.Key,
            KeyPrefix: config.KeyPrefix,
            ListPrefix: config.ListPrefix,
            Value: config.Value,
            KeepKey: KeepKey || config.KeepKey == true,
            RequestTimeoutSeconds: config.RequestTimeoutSeconds.GetValueOrDefault(8),
            RetryCount: config.RetryCount.GetValueOrDefault(2));
    }

    private static string Required(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required config value '{key}'.");
        }

        return value.Trim();
    }
}

internal sealed record ResolvedKvProbeOptions(
    string ConfigPath,
    string ApiBaseUrl,
    string ApiToken,
    string AccountId,
    string NamespaceId,
    string? Key,
    string? KeyPrefix,
    string? ListPrefix,
    string? Value,
    bool KeepKey,
    int RequestTimeoutSeconds,
    int RetryCount);

internal sealed record KvProbeConfig(
    [property: JsonPropertyName("apiBaseUrl")] string? ApiBaseUrl,
    [property: JsonPropertyName("apiToken")] string? ApiToken,
    [property: JsonPropertyName("accountId")] string? AccountId,
    [property: JsonPropertyName("namespaceId")] string? NamespaceId,
    [property: JsonPropertyName("key")] string? Key,
    [property: JsonPropertyName("keyPrefix")] string? KeyPrefix,
    [property: JsonPropertyName("listPrefix")] string? ListPrefix,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("keepKey")] bool? KeepKey,
    [property: JsonPropertyName("requestTimeoutSeconds")] int? RequestTimeoutSeconds,
    [property: JsonPropertyName("retryCount")] int? RetryCount);
