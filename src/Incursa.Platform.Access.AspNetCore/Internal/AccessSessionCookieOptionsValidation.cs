namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.Extensions.Options;

internal sealed class AccessSessionCookieOptionsValidation : IValidateOptions<AccessSessionCookieOptions>
{
    public ValidateOptionsResult Validate(string? name, AccessSessionCookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.AuthenticationScheme))
        {
            failures.Add("AuthenticationScheme is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AuthenticationCookieName))
        {
            failures.Add("AuthenticationCookieName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SessionCookieName))
        {
            failures.Add("SessionCookieName is required.");
        }

        if (options.ExpireTimeSpan <= TimeSpan.Zero)
        {
            failures.Add("ExpireTimeSpan must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
