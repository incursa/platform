namespace Incursa.Platform.Access.Razor;

public static class AccessAuthenticationRequestHelpers
{
    public static AccessAuthenticationRequestMetadata BuildMetadata(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return new AccessAuthenticationRequestMetadata(
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers.UserAgent.ToString());
    }

    public static string? NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        if (!returnUrl.StartsWith("/", StringComparison.Ordinal) || returnUrl.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        return returnUrl;
    }

    public static string BuildAppAbsoluteUrl(HttpRequest request, string? publicBaseUrl, string path)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return Combine(publicBaseUrl, path);
        }

        var host = request.Host.HasValue ? request.Host.Value : "localhost";
        var baseUrl = $"{request.Scheme}://{host}";
        return Combine(baseUrl, path);
    }

    private static string Combine(string baseUrl, string path)
    {
        var root = baseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(path))
        {
            return root;
        }

        return path.StartsWith("/", StringComparison.Ordinal)
            ? $"{root}{path}"
            : $"{root}/{path}";
    }
}
