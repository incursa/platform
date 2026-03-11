namespace Incursa.Platform.Access.AspNetCore.Tests;

using Incursa.Platform.Access.Razor;

[Trait("Category", "Unit")]
public sealed class AccessAuthenticationUiOptionsTests
{
    [Fact]
    public void RouteDefaults_MatchPackagedAuthPageTemplates()
    {
        var routes = new AccessAuthenticationUiOptions().Routes;

        routes.SignInPath.ShouldBe("/auth/sign-in");
        routes.CallbackPath.ShouldBe("/auth/callback");
        routes.MagicPath.ShouldBe("/auth/magic");
        routes.MagicVerifyPath.ShouldBe("/auth/magic/verify");
        routes.VerifyEmailPath.ShouldBe("/auth/verify-email");
        routes.MfaSetupPath.ShouldBe("/auth/mfa/setup");
        routes.MfaVerifyPath.ShouldBe("/auth/mfa/verify");
        routes.OrganizationSelectionPath.ShouldBe("/auth/organizations/select");
        routes.ForgotPasswordPath.ShouldBe("/auth/password/forgot");
        routes.ResetPasswordPath.ShouldBe("/auth/password/reset");
        routes.ErrorPath.ShouldBe("/auth/error");
        routes.AccessDeniedPath.ShouldBe("/auth/access-denied");
        routes.LoggedOutPath.ShouldBe("/auth/logged-out");
        routes.SessionExpiredPath.ShouldBe("/auth/session-expired");
    }
}
