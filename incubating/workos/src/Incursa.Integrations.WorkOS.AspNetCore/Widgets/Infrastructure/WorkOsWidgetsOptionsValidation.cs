namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.Infrastructure;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Microsoft.Extensions.Options;

internal sealed class WorkOsWidgetsOptionsValidation : IValidateOptions<WorkOsWidgetsOptions>
{
    public ValidateOptionsResult Validate(string? name, WorkOsWidgetsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("WorkOS widgets API key is required.");
        }

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("WorkOS widgets ApiBaseUrl must be an absolute URI.");
        }

        if (!string.IsNullOrWhiteSpace(options.TextDirection)
            && !string.Equals(options.TextDirection, "ltr", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(options.TextDirection, "rtl", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("WorkOS widgets TextDirection must be 'ltr' or 'rtl'.");
        }

        if (options.DialogZIndex is <= 0)
        {
            return ValidateOptionsResult.Fail("WorkOS widgets DialogZIndex must be greater than 0.");
        }

        return ValidateOptionsResult.Success;
    }
}
