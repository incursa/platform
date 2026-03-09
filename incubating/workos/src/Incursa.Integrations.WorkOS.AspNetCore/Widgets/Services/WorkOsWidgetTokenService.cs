namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;

using System.Net.Http.Headers;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Microsoft.Extensions.Options;

internal sealed class WorkOsWidgetTokenService : IWorkOsWidgetTokenService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;
    private readonly WorkOsWidgetsOptions widgetsOptions;
    private readonly IWorkOsWidgetScopeProvider scopeProvider;

    public WorkOsWidgetTokenService(
        HttpClient httpClient,
        IOptions<WorkOsWidgetsOptions> options,
        IWorkOsWidgetScopeProvider scopeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scopeProvider);

        this.httpClient = httpClient;
        this.widgetsOptions = options.Value;
        this.scopeProvider = scopeProvider;
    }

    public async Task<string> CreateTokenAsync(WorkOsWidgetType widgetType, WorkOsWidgetIdentity identity, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            organization_id = identity.OrganizationId,
            user_id = identity.UserId,
            scopes = scopeProvider.GetScopes(widgetType),
        };

        var baseUri = new Uri(widgetsOptions.ApiBaseUrl, UriKind.Absolute);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/widgets/token"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", widgetsOptions.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"WorkOS token request failed with {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        var payload = JsonSerializer.Deserialize<WidgetTokenResponse>(responseBody, SerializerOptions);
        var token = payload?.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            token = TryGetTokenFromRawPayload(responseBody);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("WorkOS token response did not include a token.");
        }

        return token;
    }

    private static string? TryGetTokenFromRawPayload(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        using var document = JsonDocument.Parse(responseBody);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (document.RootElement.TryGetProperty("token", out var tokenProp)
            && tokenProp.ValueKind == JsonValueKind.String)
        {
            return tokenProp.GetString();
        }

        if (document.RootElement.TryGetProperty("auth_token", out var authTokenProp)
            && authTokenProp.ValueKind == JsonValueKind.String)
        {
            return authTokenProp.GetString();
        }

        return null;
    }

    private sealed class WidgetTokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}
