namespace Incursa.Integrations.WorkOS.Access;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Microsoft.Extensions.Options;

internal sealed class WorkOsAuthOptionsValidation : IValidateOptions<WorkOsAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, WorkOsAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("ApiBaseUrl must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add("ClientId is required.");
        }

        if (!string.IsNullOrWhiteSpace(options.AuthApiBaseUrl)
            && !Uri.TryCreate(options.AuthApiBaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("AuthApiBaseUrl must be an absolute URI when provided.");
        }

        if (!string.IsNullOrWhiteSpace(options.Issuer)
            && !Uri.TryCreate(options.Issuer, UriKind.Absolute, out _))
        {
            failures.Add("Issuer must be an absolute URI when provided.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey)
            && string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            failures.Add("ApiKey or ClientSecret is required.");
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            failures.Add("RequestTimeout must be greater than zero.");
        }

        if (options.JwksCacheDuration <= TimeSpan.Zero)
        {
            failures.Add("JwksCacheDuration must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
