#pragma warning disable MA0048
namespace Incursa.Platform.Access.Razor;

public sealed class AccessAuthenticationUiOptions
{
    public const string SectionName = "Incursa:Platform:Access:AuthUi";

    public string AuthenticationScheme { get; set; } = "Access";

    public string DefaultReturnUrl { get; set; } = "/";

    public string? PublicBaseUrl { get; set; }

    public string CookiePrefix { get; set; } = "__Host-incursa-access-auth";

    public bool IsConfigured { get; set; } = true;

    public bool EnablePassword { get; set; } = true;

    public bool EnablePasswordRecovery { get; set; } = true;

    public bool EnableMagicAuth { get; set; } = true;

    public string TotpIssuer { get; set; } = "Incursa";

    public AccessAuthenticationBrandingOptions Branding { get; set; } = new();

    public AccessAuthenticationSetupOptions Setup { get; set; } = new();

    public AccessAuthenticationRouteOptions Routes { get; set; } = new();

    public ICollection<AccessAuthenticationProviderOptions> Providers { get; set; } = [];
}

public sealed class AccessAuthenticationBrandingOptions
{
    public string ApplicationName { get; set; } = "Incursa";

    public string Eyebrow { get; set; } = "Access";

    public string Headline { get; set; } = "Access your workspace";

    public string SupportingText { get; set; } = "Choose a sign-in method that matches your account.";

    public string SidebarLabel { get; set; } = "Incursa";

    public string SidebarHeadline { get; set; } = "Secure access, kept inside the product.";

    public string SidebarNote { get; set; } = "Authentication happens through your configured provider, while the product keeps the browser flow, verification, and organization context in one place.";

    public ICollection<AccessAuthenticationHighlightOptions> Highlights { get; set; } =
    [
        new()
        {
            Title = "One session",
            Description = "Password, magic code, MFA, and organization selection stay in the same UI.",
        },
        new()
        {
            Title = "Provider neutral",
            Description = "The same screens can sit in front of WorkOS or another Access provider.",
        },
        new()
        {
            Title = "Ready to package",
            Description = "Ship the auth experience as a reusable Razor class library instead of forking each app.",
        },
    ];
}

public sealed class AccessAuthenticationHighlightOptions
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public sealed class AccessAuthenticationSetupOptions
{
    public string BadgeText { get; set; } = "Setup required";

    public string Title { get; set; } = "Authentication is not configured.";

    public string Description { get; set; } = "This deployment is running, but the browser sign-in flow cannot start until an authentication provider is configured.";

    public string ActionHref { get; set; } = "/health";

    public string ActionLabel { get; set; } = "Health check";

    public ICollection<string> RequiredConfigurationEntries { get; set; } = [];
}

public sealed class AccessAuthenticationRouteOptions
{
    public string LoginPath { get; set; } = "/auth/login";

    public string SignInPath { get; set; } = "/auth/sign-in";

    public string CallbackPath { get; set; } = "/auth/callback";

    public string MagicPath { get; set; } = "/auth/magic";

    public string MagicVerifyPath { get; set; } = "/auth/magic/verify";

    public string VerifyEmailPath { get; set; } = "/auth/verify-email";

    public string MfaSetupPath { get; set; } = "/auth/mfa/setup";

    public string MfaVerifyPath { get; set; } = "/auth/mfa/verify";

    public string OrganizationSelectionPath { get; set; } = "/auth/organizations/select";

    public string ForgotPasswordPath { get; set; } = "/auth/password/forgot";

    public string ResetPasswordPath { get; set; } = "/auth/password/reset";

    public string ErrorPath { get; set; } = "/auth/error";

    public string AccessDeniedPath { get; set; } = "/auth/access-denied";

    public string LoggedOutPath { get; set; } = "/auth/logged-out";

    public string SessionExpiredPath { get; set; } = "/auth/session-expired";

    public string LogoutPath { get; set; } = "/auth/logout";

    public string SignOutPath { get; set; } = "/auth/sign-out";
}

public sealed class AccessAuthenticationProviderOptions
{
    public string Label { get; set; } = string.Empty;

    public string? Provider { get; set; }

    public string? ConnectionId { get; set; }

    public string? OrganizationId { get; set; }

    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Label)
            ? (Provider ?? ConnectionId ?? "Continue").Trim()
            : Label.Trim();
}
#pragma warning restore MA0048
