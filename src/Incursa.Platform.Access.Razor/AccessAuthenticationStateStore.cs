#pragma warning disable MA0048
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Access.Razor;

public sealed class AccessAuthenticationStateStore
{
    private static readonly TimeSpan RedirectStateLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PendingChallengeLifetime = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDataProtector redirectProtector;
    private readonly IDataProtector challengeProtector;
    private readonly string redirectStateCookieName;
    private readonly string pendingChallengeCookieName;

    public AccessAuthenticationStateStore(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<AccessAuthenticationUiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(options);

        var cookiePrefix = NormalizeCookiePrefix(options.Value.CookiePrefix);
        redirectStateCookieName = $"{cookiePrefix}-flow";
        pendingChallengeCookieName = $"{cookiePrefix}-challenge";
        redirectProtector = dataProtectionProvider.CreateProtector("Incursa.Platform.Access.Razor.RedirectState.v1");
        challengeProtector = dataProtectionProvider.CreateProtector("Incursa.Platform.Access.Razor.PendingChallenge.v1");
    }

    public AccessAuthorizationState CreateRedirectState(string? returnUrl)
    {
        return new AccessAuthorizationState(
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            AccessAuthenticationRequestHelpers.NormalizeReturnUrl(returnUrl),
            DateTimeOffset.UtcNow);
    }

    public void SaveRedirectState(HttpContext httpContext, AccessAuthorizationState state)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(state);

        WriteCookie(httpContext, redirectStateCookieName, redirectProtector, state, RedirectStateLifetime);
    }

    public AccessAuthorizationState? ConsumeRedirectState(HttpContext httpContext, string? state)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (string.IsNullOrWhiteSpace(state))
        {
            ClearRedirectState(httpContext);
            return null;
        }

        var stored = ReadCookie<AccessAuthorizationState>(httpContext, redirectStateCookieName, redirectProtector);
        ClearRedirectState(httpContext);
        if (stored is null)
        {
            return null;
        }

        if (!string.Equals(stored.State, state.Trim(), StringComparison.Ordinal))
        {
            return null;
        }

        if (DateTimeOffset.UtcNow - stored.CreatedUtc > RedirectStateLifetime)
        {
            return null;
        }

        return stored;
    }

    public void ClearRedirectState(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.Cookies.Delete(redirectStateCookieName, BuildCookieOptions(httpContext, null));
    }

    public AccessPendingAuthenticationState? GetPendingChallenge(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var stored = ReadCookie<AccessPendingAuthenticationState>(httpContext, pendingChallengeCookieName, challengeProtector);
        if (stored is null)
        {
            return null;
        }

        if (DateTimeOffset.UtcNow - stored.CreatedUtc > PendingChallengeLifetime)
        {
            ClearPendingChallenge(httpContext);
            return null;
        }

        return stored;
    }

    public void SavePendingChallenge(HttpContext httpContext, AccessPendingAuthenticationState state)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(state);

        WriteCookie(httpContext, pendingChallengeCookieName, challengeProtector, state, PendingChallengeLifetime);
    }

    public void ClearPendingChallenge(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.Cookies.Delete(pendingChallengeCookieName, BuildCookieOptions(httpContext, null));
    }

    private static void WriteCookie<T>(
        HttpContext httpContext,
        string cookieName,
        IDataProtector protector,
        T value,
        TimeSpan lifetime)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        var protectedPayload = protector.Protect(payload);

        httpContext.Response.Cookies.Append(
            cookieName,
            protectedPayload,
            BuildCookieOptions(httpContext, DateTimeOffset.UtcNow.Add(lifetime)));
    }

    private static T? ReadCookie<T>(HttpContext httpContext, string cookieName, IDataProtector protector)
    {
        if (!httpContext.Request.Cookies.TryGetValue(cookieName, out var payload)
            || string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        try
        {
            var json = protector.Unprotect(payload);
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }
        catch
        {
            return default;
        }
    }

    private static CookieOptions BuildCookieOptions(HttpContext httpContext, DateTimeOffset? expiresUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Path = "/",
            Expires = expiresUtc,
        };
    }

    private static string NormalizeCookiePrefix(string? cookiePrefix)
    {
        var normalized = string.IsNullOrWhiteSpace(cookiePrefix)
            ? "__Host-incursa-access-auth"
            : cookiePrefix.Trim();
        return normalized.TrimEnd('-');
    }
}

public sealed record AccessAuthorizationState(
    string State,
    string? ReturnUrl,
    DateTimeOffset CreatedUtc);

public sealed record AccessPendingAuthenticationState(
    AccessChallengeKind Kind,
    string PendingAuthenticationToken,
    string? ReturnUrl,
    string? Email,
    string? EmailVerificationId,
    string? AuthenticationChallengeId,
    string? AuthenticationFactorId,
    IReadOnlyList<AccessPendingAuthenticationFactor> Factors,
    IReadOnlyList<AccessPendingOrganizationChoice> Organizations,
    DateTimeOffset CreatedUtc)
{
    public static AccessPendingAuthenticationState FromChallenge(AccessChallenge challenge, string? returnUrl)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        return new AccessPendingAuthenticationState(
            challenge.Kind,
            challenge.PendingAuthenticationToken,
            AccessAuthenticationRequestHelpers.NormalizeReturnUrl(returnUrl),
            challenge.Email,
            challenge.EmailVerificationId,
            challenge.AuthenticationChallengeId,
            challenge.Factors.FirstOrDefault()?.Id,
            challenge.Factors.Select(static factor => new AccessPendingAuthenticationFactor(factor.Id, factor.Type)).ToArray(),
            challenge.Organizations.Select(static organization => new AccessPendingOrganizationChoice(organization.Id, organization.Name)).ToArray(),
            DateTimeOffset.UtcNow);
    }

    public bool ContainsOrganization(string organizationId) =>
        Organizations.Any(organization => string.Equals(organization.Id, organizationId, StringComparison.Ordinal));
}

public sealed record AccessPendingAuthenticationFactor(string Id, string Type);

public sealed record AccessPendingOrganizationChoice(string Id, string Name);
#pragma warning restore MA0048
