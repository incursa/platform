namespace Incursa.Platform.Access.AspNetCore;

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class CookieAccessSessionStore : IAccessSessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IDataProtector protector;
    private readonly AccessSessionCookieOptions options;
    private readonly TimeProvider timeProvider;

    public CookieAccessSessionStore(
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider dataProtectionProvider,
        IOptions<AccessSessionCookieOptions> options,
        TimeProvider timeProvider)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        ArgumentNullException.ThrowIfNull(options);

        protector = dataProtectionProvider.CreateProtector("Incursa.Platform.Access.AspNetCore.CookieAccessSessionStore");
        this.options = options.Value;
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<AccessAuthenticatedSession?> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null
            || !httpContext.Request.Cookies.TryGetValue(options.SessionCookieName, out var rawValue)
            || string.IsNullOrWhiteSpace(rawValue))
        {
            return Task.FromResult<AccessAuthenticatedSession?>(null);
        }

        try
        {
            var json = protector.Unprotect(rawValue);
            return Task.FromResult(JsonSerializer.Deserialize<AccessAuthenticatedSession>(json, SerializerOptions));
        }
        catch
        {
            httpContext.Response.Cookies.Delete(options.SessionCookieName, CreateBaseCookieOptions(httpContext));
            return Task.FromResult<AccessAuthenticatedSession?>(null);
        }
    }

    public Task SetAsync(AccessAuthenticatedSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        var httpContext = GetRequiredHttpContext();
        var payload = JsonSerializer.Serialize(session, SerializerOptions);
        var protectedPayload = protector.Protect(payload);
        httpContext.Response.Cookies.Append(
            options.SessionCookieName,
            protectedPayload,
            CreateCookieOptions(httpContext, session));
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            httpContext.Response.Cookies.Delete(options.SessionCookieName, CreateBaseCookieOptions(httpContext));
        }

        return Task.CompletedTask;
    }

    private CookieOptions CreateCookieOptions(HttpContext httpContext, AccessAuthenticatedSession session)
    {
        var cookieOptions = CreateBaseCookieOptions(httpContext);
        cookieOptions.Expires = session.RefreshTokenExpiresAtUtc
            ?? session.AccessTokenExpiresAtUtc
            ?? timeProvider.GetUtcNow().Add(options.ExpireTimeSpan);
        return cookieOptions;
    }

    private CookieOptions CreateBaseCookieOptions(HttpContext httpContext) =>
        new()
        {
            HttpOnly = true,
            IsEssential = true,
            Path = options.CookiePath,
            SameSite = options.SameSite,
            Secure = options.SecurePolicy == CookieSecurePolicy.Always
                || (options.SecurePolicy == CookieSecurePolicy.SameAsRequest && httpContext.Request.IsHttps),
        };

    private HttpContext GetRequiredHttpContext() =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("An active HttpContext is required.");
}
