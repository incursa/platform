namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class CookieOrganizationSelectionStore : IOrganizationSelectionStore
{
    private readonly WorkOsAppAuthOptions options;

    public CookieOrganizationSelectionStore(IOptions<WorkOsAppAuthOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public string? Get(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (this.options.ResolveFromCookie && context.Request.Cookies.TryGetValue(this.options.CookieOrganizationKey, out var cookieValue))
        {
            return string.IsNullOrWhiteSpace(cookieValue) ? null : cookieValue.Trim();
        }

        if (this.options.EnableSessionSelection && this.options.ResolveFromSession)
        {
            return context.Session.GetString(this.options.SessionOrganizationKey);
        }

        return null;
    }

    public void Set(HttpContext context, string organizationId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        context.Response.Cookies.Append(this.options.CookieOrganizationKey, organizationId.Trim(), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(365),
        });

        if (this.options.EnableSessionSelection)
        {
            context.Session.SetString(this.options.SessionOrganizationKey, organizationId.Trim());
        }
    }

    public void Clear(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.Cookies.Delete(this.options.CookieOrganizationKey);
        if (this.options.EnableSessionSelection)
        {
            context.Session.Remove(this.options.SessionOrganizationKey);
        }
    }
}
