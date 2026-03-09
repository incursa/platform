namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public enum OrganizationIdentifierPreference
{
    WorkOsId = 0,
    ExternalIdPreferred = 1,
}

public enum OrganizationSwitcherRedirectMode
{
    Template = 0,
    FixedRoute = 1,
}
