namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsAuthOptions
{
    public string ApiBaseUrl { get; set; } = "https://api.workos.com";

    public string? AuthApiBaseUrl { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string? Issuer { get; set; }

    public IReadOnlyList<string> ExpectedAudiences { get; set; } = Array.Empty<string>();

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan JwksCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

    public string AuthorizationPath { get; set; } = "/user_management/authorize";

    public string AuthenticatePath { get; set; } = "/user_management/authenticate";

    public string MagicAuthPath { get; set; } = "/user_management/magic_auth";

    public string TotpEnrollPath { get; set; } = "/auth/factors/enroll";

    public string SessionRevokePathTemplate { get; set; } = "/user_management/sessions/{sessionId}/revoke";

    public string LogoutPath { get; set; } = "/user_management/sessions/logout";

    public Uri GetApiBaseUri() => CreateAbsoluteUri(ApiBaseUrl);

    public Uri GetAuthApiBaseUri() => CreateAbsoluteUri(string.IsNullOrWhiteSpace(AuthApiBaseUrl) ? ApiBaseUrl : AuthApiBaseUrl);

    public Uri GetIssuerUri() => CreateAbsoluteUri(string.IsNullOrWhiteSpace(Issuer) ? GetAuthApiBaseUri().ToString() : Issuer);

    public Uri GetJwksUri() => new(GetIssuerUri(), "/sso/jwks/" + Uri.EscapeDataString(ClientId.Trim()));

    public string GetEffectiveClientSecret() => FirstNonEmpty(ClientSecret, ApiKey)
        ?? throw new InvalidOperationException("A WorkOS client secret or API key must be configured.");

    public string GetEffectiveApiKey() => FirstNonEmpty(ApiKey, ClientSecret)
        ?? throw new InvalidOperationException("A WorkOS API key or client secret must be configured.");

    private static Uri CreateAbsoluteUri(string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new Uri(value.Trim().TrimEnd('/') + "/", UriKind.Absolute);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim();
}
