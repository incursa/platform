namespace Incursa.Integrations.WorkOS.Core.Claims;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsClientCredentialsTokenProvider : IWorkOsAccessTokenProvider, IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _httpClient;
    private readonly WorkOsClientCredentialsOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc;

    public WorkOsClientCredentialsTokenProvider(HttpClient httpClient, WorkOsClientCredentialsOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Authority))
        {
            throw new ArgumentException("Authority is required for WorkOS client credentials flow.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new ArgumentException("ClientId is required for WorkOS client credentials flow.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new ArgumentException("ClientSecret is required for WorkOS client credentials flow.", nameof(options));
        }

        _httpClient = httpClient;
        _options = options;
        _httpClient.Timeout = options.TokenFetchTimeout;
        _httpClient.BaseAddress = new Uri(options.Authority.TrimEnd('/') + "/", UriKind.Absolute);
    }

    public async ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(_accessToken) &&
            _expiresAtUtc > now.Add(_options.TokenMinRefreshBeforeExpiry))
        {
            return _accessToken;
        }

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(_accessToken) &&
                _expiresAtUtc > now.Add(_options.TokenMinRefreshBeforeExpiry))
            {
                return _accessToken;
            }

            var attempts = Math.Max(1, _options.RetryCount);
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    var token = await FetchAsync(ct).ConfigureAwait(false);
                    _accessToken = token.AccessToken;
                    _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, token.ExpiresInSeconds));
                    return _accessToken;
                }
                catch when (attempt < attempts)
                {
                    var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _gate.Release();
        }

        throw new InvalidOperationException("Unable to fetch a WorkOS access token.");
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private async ValueTask<TokenResponse> FetchAsync(CancellationToken ct)
    {
        Dictionary<string, string?> formValues = new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["scope"] = _options.Scope,
        };

        using var content = new FormUrlEncodedContent(formValues);

        using var response = await _httpClient.PostAsync(_options.TokenEndpointPath, content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"WorkOS token request failed: {(int)response.StatusCode} {response.ReasonPhrase}. body={body}");
        }

        var parsed = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, ct).ConfigureAwait(false);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.AccessToken))
        {
            throw new InvalidOperationException("WorkOS token response was empty.");
        }

        return parsed;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        required public string AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds { get; init; }
    }
}
