namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;

internal static class WorkOsWidgetTypeExtensions
{
    public static string ToWidgetName(this WorkOsWidgetType widgetType)
    {
        return widgetType switch
        {
            WorkOsWidgetType.UsersManagement => "user-management",
            WorkOsWidgetType.UserProfile => "user-profile",
            WorkOsWidgetType.UserSessions => "user-sessions",
            WorkOsWidgetType.UserSecurity => "user-security",
            WorkOsWidgetType.ApiKeys => "api-keys",
            WorkOsWidgetType.Pipes => "pipes",
            WorkOsWidgetType.DomainVerification => "admin-portal-domain-verification",
            WorkOsWidgetType.SsoConnection => "admin-portal-sso-connection",
            WorkOsWidgetType.OrganizationSwitcher => "organization-switcher",
            _ => throw new ArgumentOutOfRangeException(nameof(widgetType), widgetType, "Unsupported WorkOS widget type."),
        };
    }
}
