namespace Incursa.Integrations.WorkOS.Webhooks;

/// <summary>
/// Configuration for the WorkOS webhook adapter.
/// </summary>
public sealed class WorkOsWebhookOptions
{
    /// <summary>
    /// Gets or sets the WorkOS webhook signing secret used to validate inbound requests.
    /// </summary>
    public string SigningSecret { get; set; } = string.Empty;
}
