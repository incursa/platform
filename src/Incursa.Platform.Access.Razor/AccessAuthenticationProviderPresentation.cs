namespace Incursa.Platform.Access.Razor;

public sealed class AccessAuthenticationProviderPresentation
{
    private AccessAuthenticationProviderPresentation(
        AccessAuthenticationProviderOptions options,
        string key,
        string displayLabel,
        string description,
        string themeClass,
        string iconSvg)
    {
        Options = options;
        Key = key;
        DisplayLabel = displayLabel;
        Description = description;
        ThemeClass = themeClass;
        IconSvg = iconSvg;
    }

    public AccessAuthenticationProviderOptions Options { get; }

    public string Key { get; }

    public string DisplayLabel { get; }

    public string Description { get; }

    public string ThemeClass { get; }

    public string IconSvg { get; }

    public static AccessAuthenticationProviderPresentation Create(AccessAuthenticationProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var providerKey = ResolveProviderKey(options);
        var label = options.DisplayLabel;
        var displayLabel = label.Contains(' ', StringComparison.Ordinal)
            || label.StartsWith("Continue", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("Sign in", StringComparison.OrdinalIgnoreCase)
                ? label
                : $"Continue with {label}";

        return new AccessAuthenticationProviderPresentation(
            options,
            providerKey,
            displayLabel,
            BuildDescription(providerKey),
            providerKey,
            BuildIconSvg(providerKey));
    }

    private static string ResolveProviderKey(AccessAuthenticationProviderOptions options)
    {
        var signature = string.Join(
            ' ',
            new[]
            {
                options.Label,
                options.Provider,
                options.ConnectionId,
            }.Where(static value => !string.IsNullOrWhiteSpace(value)))
            .ToLowerInvariant();

        if (signature.Contains("google", StringComparison.Ordinal))
        {
            return "google";
        }

        if (signature.Contains("microsoft", StringComparison.Ordinal)
            || signature.Contains("azure", StringComparison.Ordinal)
            || signature.Contains("entra", StringComparison.Ordinal)
            || signature.Contains("office", StringComparison.Ordinal))
        {
            return "microsoft";
        }

        if (signature.Contains("apple", StringComparison.Ordinal))
        {
            return "apple";
        }

        if (signature.Contains("github", StringComparison.Ordinal))
        {
            return "github";
        }

        if (signature.Contains("okta", StringComparison.Ordinal))
        {
            return "okta";
        }

        if (signature.Contains("saml", StringComparison.Ordinal)
            || signature.Contains("oidc", StringComparison.Ordinal)
            || signature.Contains("sso", StringComparison.Ordinal))
        {
            return "sso";
        }

        return "generic";
    }

    private static string BuildDescription(string providerKey) =>
        providerKey switch
        {
            "google" => "Use your Google Workspace identity.",
            "microsoft" => "Use your Microsoft 365 or Entra account.",
            "apple" => "Use the Apple account already linked to you.",
            "github" => "Use your GitHub identity to continue.",
            "okta" => "Use your Okta-managed organization login.",
            "sso" => "Continue with your organization's identity provider.",
            _ => "Use the single sign-on connection already assigned to you.",
        };

    private static string BuildIconSvg(string providerKey) =>
        providerKey switch
        {
            "google" => """
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path fill="#4285F4" d="M21.6 12.23c0-.73-.06-1.43-.2-2.1H12v3.98h5.39a4.63 4.63 0 0 1-2 3.04v2.52h3.24c1.9-1.76 2.97-4.34 2.97-7.44Z"/>
                    <path fill="#34A853" d="M12 22c2.7 0 4.97-.9 6.63-2.43l-3.24-2.52c-.9.61-2.04.97-3.39.97-2.61 0-4.82-1.76-5.61-4.12H3.05v2.6A10 10 0 0 0 12 22Z"/>
                    <path fill="#FBBC04" d="M6.39 13.9A5.98 5.98 0 0 1 6.08 12c0-.66.11-1.3.31-1.9V7.5H3.05A10 10 0 0 0 2 12c0 1.61.39 3.14 1.05 4.5l3.34-2.6Z"/>
                    <path fill="#EA4335" d="M12 5.98c1.47 0 2.79.5 3.82 1.49l2.86-2.86C16.96 3 14.7 2 12 2A10 10 0 0 0 3.05 7.5l3.34 2.6C7.18 7.74 9.39 5.98 12 5.98Z"/>
                </svg>
                """,
            "microsoft" => """
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <rect x="3" y="3" width="8" height="8" fill="#F25022"/>
                    <rect x="13" y="3" width="8" height="8" fill="#7FBA00"/>
                    <rect x="3" y="13" width="8" height="8" fill="#00A4EF"/>
                    <rect x="13" y="13" width="8" height="8" fill="#FFB900"/>
                </svg>
                """,
            "apple" => """
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path fill="currentColor" d="M15.12 2.7c0 1.06-.4 2.04-1.08 2.74-.75.77-1.99 1.33-3.05 1.24-.13-1.01.37-2.08 1.08-2.82.78-.8 2.09-1.39 3.05-1.16Zm3.47 15.09c-.5 1.14-.73 1.65-1.37 2.63-.88 1.34-2.13 3.01-3.68 3.02-1.38.01-1.73-.9-3.61-.9-1.87 0-2.26.88-3.63.89-1.55.02-2.73-1.48-3.61-2.82-2.46-3.76-2.71-8.17-1.2-10.49 1.07-1.65 2.76-2.62 4.35-2.62 1.67 0 2.72.91 4.1.91 1.34 0 2.15-.91 4.09-.91 1.41 0 2.9.77 3.97 2.1-3.5 1.92-2.93 6.97.59 8.19Z"/>
                </svg>
                """,
            "github" => """
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path fill="currentColor" d="M12 2C6.48 2 2 6.58 2 12.23c0 4.51 2.87 8.34 6.84 9.69.5.1.68-.22.68-.49 0-.24-.01-1.03-.01-1.87-2.78.62-3.37-1.21-3.37-1.21-.46-1.18-1.11-1.49-1.11-1.49-.91-.64.07-.63.07-.63 1 .07 1.53 1.06 1.53 1.06.9 1.57 2.36 1.12 2.94.86.09-.67.35-1.12.64-1.38-2.22-.26-4.56-1.14-4.56-5.08 0-1.12.39-2.04 1.03-2.76-.1-.26-.45-1.3.1-2.71 0 0 .84-.28 2.75 1.05A9.3 9.3 0 0 1 12 6.9c.85 0 1.7.12 2.49.36 1.9-1.33 2.74-1.05 2.74-1.05.56 1.41.21 2.45.11 2.71.64.72 1.03 1.64 1.03 2.76 0 3.95-2.34 4.81-4.57 5.07.36.32.68.95.68 1.92 0 1.39-.01 2.51-.01 2.85 0 .27.18.6.69.49A10.25 10.25 0 0 0 22 12.23C22 6.58 17.52 2 12 2Z"/>
                </svg>
                """,
            "okta" => """
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <circle cx="12" cy="12" r="8" fill="none" stroke="currentColor" stroke-width="2"/>
                    <circle cx="12" cy="12" r="3" fill="currentColor"/>
                </svg>
                """,
            "sso" => """
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path fill="currentColor" d="M4 8.5A2.5 2.5 0 0 1 6.5 6H10v3H7v8h3v3H6.5A2.5 2.5 0 0 1 4 17.5v-9Zm10-2.5h3.5A2.5 2.5 0 0 1 20 8.5v9a2.5 2.5 0 0 1-2.5 2.5H14v-3h3V9h-3V6Zm-5 5h6v2H9v-2Z"/>
                </svg>
                """,
            _ => """
                <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <path fill="currentColor" d="M12 3 4 7v4c0 5.05 3.4 9.78 8 11 4.6-1.22 8-5.95 8-11V7l-8-4Zm0 2.2 5.5 2.75V11c0 3.92-2.53 7.92-5.5 9-2.97-1.08-5.5-5.08-5.5-9V7.95L12 5.2Zm0 3.3a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5Zm-4 8.5c1.02-1.6 2.5-2.4 4-2.4s2.98.8 4 2.4H8Z"/>
                </svg>
                """,
        };
}
