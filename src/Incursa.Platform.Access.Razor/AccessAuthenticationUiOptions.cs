#pragma warning disable MA0048
namespace Incursa.Platform.Access.Razor;

public sealed class AccessAuthenticationUiOptions
{
    public const string SectionName = "Incursa:Platform:Access:AuthUi";

    public string DefaultReturnUrl { get; set; } = "/";

    public string? PublicBaseUrl { get; set; }

    public string CookiePrefix { get; set; } = "__Host-incursa-access-auth";

    public bool EnablePassword { get; set; } = true;

    public bool EnableMagicAuth { get; set; } = true;

    public string TotpIssuer { get; set; } = "Incursa";

    public AccessAuthenticationBrandingOptions Branding { get; set; } = new();

    public AccessAuthenticationRouteOptions Routes { get; set; } = new();

    public ICollection<AccessAuthenticationProviderOptions> Providers { get; set; } = [];
}

public sealed class AccessAuthenticationBrandingOptions
{
    public string ApplicationName { get; set; } = "Incursa";

    public string Headline { get; set; } = "Access your workspace";

    public string SupportingText { get; set; } = "Choose a sign-in method that matches your account.";
}

public sealed class AccessAuthenticationRouteOptions
{
    public string SignInPath { get; set; } = "/auth/sign-in";

    public string CallbackPath { get; set; } = "/auth/callback";

    public string MagicPath { get; set; } = "/auth/magic";

    public string MagicVerifyPath { get; set; } = "/auth/magic/verify";

    public string VerifyEmailPath { get; set; } = "/auth/verify-email";

    public string MfaSetupPath { get; set; } = "/auth/mfa/setup";

    public string MfaVerifyPath { get; set; } = "/auth/mfa/verify";

    public string OrganizationSelectionPath { get; set; } = "/auth/organizations/select";

    public string ForgotPasswordPath { get; set; } = "/auth/forgot-password";

    public string ResetPasswordPath { get; set; } = "/auth/reset-password";

    public string ErrorPath { get; set; } = "/auth/error";

    public string LoggedOutPath { get; set; } = "/auth/logged-out";

    public string SessionExpiredPath { get; set; } = "/auth/session-expired";

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
