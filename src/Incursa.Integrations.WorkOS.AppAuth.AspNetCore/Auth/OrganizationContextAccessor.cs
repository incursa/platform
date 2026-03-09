namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;

internal sealed class OrganizationContextAccessor : IOrganizationContextAccessor, IOrganizationContextSetter
{
    public OrganizationContext? Current { get; private set; }

    public void Set(OrganizationContext? context)
    {
        Current = context;
    }
}
