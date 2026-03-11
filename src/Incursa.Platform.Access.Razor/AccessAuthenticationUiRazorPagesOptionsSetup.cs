namespace Incursa.Platform.Access.Razor;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

internal sealed class AccessAuthenticationUiRazorPagesOptionsSetup(IOptions<AccessAuthenticationUiOptions> uiOptions)
    : IConfigureOptions<RazorPagesOptions>
{
    public void Configure(RazorPagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var routes = uiOptions.Value.Routes;

        AddAlias(options, "/Auth/SignIn", routes.SignInPath, "/auth/sign-in");
        AddAlias(options, "/Auth/Callback", routes.CallbackPath, "/auth/callback");
        AddAlias(options, "/Auth/Magic", routes.MagicPath, "/auth/magic");
        AddAlias(options, "/Auth/MagicVerify", routes.MagicVerifyPath, "/auth/magic/verify");
        AddAlias(options, "/Auth/VerifyEmail", routes.VerifyEmailPath, "/auth/verify-email");
        AddAlias(options, "/Auth/MfaSetup", routes.MfaSetupPath, "/auth/mfa/setup");
        AddAlias(options, "/Auth/MfaVerify", routes.MfaVerifyPath, "/auth/mfa/verify");
        AddAlias(options, "/Auth/Organizations/Select", routes.OrganizationSelectionPath, "/auth/organizations/select");
        AddAlias(options, "/Auth/ForgotPassword", routes.ForgotPasswordPath, "/auth/password/forgot");
        AddAlias(options, "/Auth/ResetPassword", routes.ResetPasswordPath, "/auth/password/reset");
        AddAlias(options, "/Auth/Error", routes.ErrorPath, "/auth/error");
        AddAlias(options, "/Auth/AccessDenied", routes.AccessDeniedPath, "/auth/access-denied");
        AddAlias(options, "/Auth/LoggedOut", routes.LoggedOutPath, "/auth/logged-out");
        AddAlias(options, "/Auth/SessionExpired", routes.SessionExpiredPath, "/auth/session-expired");
    }

    private static void AddAlias(
        RazorPagesOptions options,
        string pageName,
        string? configuredPath,
        string defaultPath)
    {
        var normalizedPath = NormalizePath(configuredPath, defaultPath);
        if (string.Equals(normalizedPath, defaultPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        options.Conventions.AddPageRoute(pageName, normalizedPath.TrimStart('/'));
    }

    private static string NormalizePath(string? configuredPath, string defaultPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return defaultPath;
        }

        return configuredPath.StartsWith("/", StringComparison.Ordinal)
            ? configuredPath
            : "/" + configuredPath;
    }
}
