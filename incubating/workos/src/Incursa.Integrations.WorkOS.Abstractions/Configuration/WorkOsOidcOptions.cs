namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsOidcOptions
{
    public bool Enabled { get; set; }

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackPath { get; set; } = "/auth/callback";

    public string SignedOutCallbackPath { get; set; } = "/auth/signedout";

    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    public bool RequireHttpsMetadata { get; set; } = true;
}
