namespace Incursa.Platform.Access.AspNetCore.Tests;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

[Trait("Category", "Unit")]
public sealed class AccessAuthenticationAspNetCoreTests
{
    [Fact]
    public void GetAccessContext_ReadsRepeatedAndJsonArrayClaims()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim("sub", "user-1"),
            new Claim("sid", "session-1"),
            new Claim("org_ids", "[\"org-2\",\"org-1\"]"),
            new Claim("roles", "[\"owner\",\"admin\"]"),
            new Claim("role", "admin"),
            new Claim("permissions", "[\"users:write\",\"tenants:read\"]"),
            new Claim("permission", "audit:read"),
            new Claim("feature_flags", "[\"beta-dashboard\"]"),
            new Claim("entitlements", "tier:pro"),
        ], authenticationType: "Test"));

        var context = principal.GetAccessContext();

        context.ShouldNotBeNull();
        context.SubjectId.ShouldBe("user-1");
        context.SessionId.ShouldBe("session-1");
        context.OrganizationId.ShouldBe("org-1");
        context.Roles.ShouldBe(["admin", "owner"]);
        context.Permissions.ShouldBe(["audit:read", "tenants:read", "users:write"]);
        context.FeatureFlags.ShouldBe(["beta-dashboard"]);
        context.Entitlements.ShouldBe(["tier:pro"]);
    }

    [Fact]
    public async Task CookieAccessSessionStore_RoundTripsAndClearsSessionAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var cookieOptions = new AccessSessionCookieOptions();
        var directory = Path.Combine(Path.GetTempPath(), "access-session-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var store = new CookieAccessSessionStore(
                httpContextAccessor,
                DataProtectionProvider.Create(new DirectoryInfo(directory)),
                Options.Create(cookieOptions),
                TimeProvider.System);

            var session = new AccessAuthenticatedSession(
                "user-1",
                "access-token",
                "refresh-token",
                "session-1",
                "org-1",
                ["admin"],
                ["audit:read"],
                ["beta-dashboard"],
                ["tier:pro"],
                "ada@example.com",
                "Ada Lovelace",
                true,
                accessTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(5),
                refreshTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1));

            await store.SetAsync(session, cancellationToken);

            var setCookieHeader = httpContextAccessor.HttpContext!.Response.Headers.SetCookie.ToString();
            setCookieHeader.ShouldContain(cookieOptions.SessionCookieName + "=");
            setCookieHeader.ToLowerInvariant().ShouldContain("httponly");
            setCookieHeader.ToLowerInvariant().ShouldContain("secure");

            var protectedValue = ExtractCookieValue(setCookieHeader, cookieOptions.SessionCookieName);
            var readContext = new DefaultHttpContext();
            readContext.Request.Headers.Cookie = cookieOptions.SessionCookieName + "=" + protectedValue;
            httpContextAccessor.HttpContext = readContext;

            var restored = await store.GetAsync(cancellationToken);

            restored.ShouldNotBeNull();
            restored.SubjectId.ShouldBe(session.SubjectId);
            restored.AccessToken.ShouldBe(session.AccessToken);
            restored.RefreshToken.ShouldBe(session.RefreshToken);
            restored.SessionId.ShouldBe(session.SessionId);
            restored.OrganizationId.ShouldBe(session.OrganizationId);
            restored.Roles.ShouldBe(session.Roles);
            restored.Permissions.ShouldBe(session.Permissions);
            restored.FeatureFlags.ShouldBe(session.FeatureFlags);
            restored.Entitlements.ShouldBe(session.Entitlements);
            restored.Email.ShouldBe(session.Email);
            restored.DisplayName.ShouldBe(session.DisplayName);
            restored.EmailVerified.ShouldBe(session.EmailVerified);
            restored.AccessContext.SubjectId.ShouldBe(session.AccessContext.SubjectId);
            restored.AccessContext.SessionId.ShouldBe(session.AccessContext.SessionId);
            restored.AccessContext.OrganizationId.ShouldBe(session.AccessContext.OrganizationId);

            await store.ClearAsync(cancellationToken);
            readContext.Response.Headers.SetCookie.ToString().ShouldContain(cookieOptions.SessionCookieName + "=;");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SignOutAsync_ClearsSessionStoreAndDelegatesProviderLogoutAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var principalFactory = Substitute.For<IAccessClaimsPrincipalFactory>();
        var sessionStore = Substitute.For<IAccessSessionStore>();
        var authenticationService = Substitute.For<IAccessAuthenticationService>();
        var aspNetAuthenticationService = Substitute.For<IAuthenticationService>();
        var currentSession = new AccessAuthenticatedSession("user-1", "access-token", sessionId: "session-1");

        sessionStore.GetAsync(Arg.Any<CancellationToken>()).Returns(currentSession);
        authenticationService.SignOutAsync(Arg.Any<AccessSignOutRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AccessSignOutResult(true, new Uri("https://auth.example.test/logout")));

        var services = new ServiceCollection()
            .AddSingleton(aspNetAuthenticationService)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
        };

        var ticketService = new AccessAuthenticationTicketService(
            principalFactory,
            sessionStore,
            Options.Create(new AccessSessionCookieOptions { AuthenticationScheme = "AccessAuth" }),
            [authenticationService]);

        var result = await ticketService.SignOutAsync(httpContext, cancellationToken: cancellationToken);

        result.ProviderSessionRevoked.ShouldBeTrue();
        result.LogoutUrl.ShouldBe(new Uri("https://auth.example.test/logout"));

        await authenticationService.Received(1).SignOutAsync(
            Arg.Is<AccessSignOutRequest>(request => request.SessionId == "session-1" && request.ReturnToUri == null),
            Arg.Any<CancellationToken>());
        await sessionStore.Received(1).ClearAsync(Arg.Any<CancellationToken>());
        await aspNetAuthenticationService.Received(1).SignOutAsync(httpContext, "AccessAuth", null);
    }

    [Fact]
    public void AccessSessionCookieOptionsValidation_RejectsMissingNamesAndInvalidLifetime()
    {
        var validation = new AccessSessionCookieOptionsValidation().Validate(
            null,
            new AccessSessionCookieOptions
            {
                AuthenticationScheme = "",
                AuthenticationCookieName = "",
                SessionCookieName = "",
                ExpireTimeSpan = TimeSpan.Zero,
            });

        validation.Failed.ShouldBeTrue();
        validation.Failures.ShouldContain("AuthenticationScheme is required.");
        validation.Failures.ShouldContain("AuthenticationCookieName is required.");
        validation.Failures.ShouldContain("SessionCookieName is required.");
        validation.Failures.ShouldContain("ExpireTimeSpan must be greater than zero.");
    }

    private static string ExtractCookieValue(string setCookieHeader, string cookieName)
    {
        var prefix = cookieName + "=";
        var start = setCookieHeader.IndexOf(prefix, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);

        start += prefix.Length;
        var end = setCookieHeader.IndexOf(';', start);
        return end >= 0
            ? setCookieHeader[start..end]
            : setCookieHeader[start..];
    }
}
