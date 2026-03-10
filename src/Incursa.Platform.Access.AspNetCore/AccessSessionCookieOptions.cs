namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

public sealed class AccessSessionCookieOptions
{
    public string AuthenticationScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;

    public string AuthenticationCookieName { get; set; } = "__Host-incursa-access-auth";

    public string SessionCookieName { get; set; } = "__Host-incursa-access-session";

    public string CookiePath { get; set; } = "/";

    public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.Always;

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

    public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.FromDays(14);

    public bool SlidingExpiration { get; set; }
}
