namespace Incursa.Integrations.WorkOS.Webhooks;

/// <summary>
/// Shared constants for the WorkOS webhook adapter.
/// </summary>
public static class WorkOsWebhookDefaults
{
    /// <summary>
    /// The provider name used by the WorkOS webhook adapter.
    /// </summary>
    public const string ProviderName = "workos";

    /// <summary>
    /// The HTTP header containing the WorkOS webhook signature.
    /// </summary>
    public const string SignatureHeaderName = "WorkOS-Signature";
}
