namespace Incursa.Integrations.WorkOS.AppAuth.Abstractions;

public interface IOrganizationContextAccessor
{
    OrganizationContext? Current { get; }
}

public interface IOrganizationContextSetter
{
    void Set(OrganizationContext? context);
}
