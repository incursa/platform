namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class OrganizationSwitcherDefaultsOptions
{
    public OrganizationIdentifierPreference IdentifierPreference { get; set; } = OrganizationIdentifierPreference.WorkOsId;

    public OrganizationSwitcherRedirectMode RedirectMode { get; set; } = OrganizationSwitcherRedirectMode.Template;

    public bool PreserveCurrentPath { get; set; } = true;

    public string? DefaultTemplate { get; set; }

    public string? FixedRoute { get; set; }
}
