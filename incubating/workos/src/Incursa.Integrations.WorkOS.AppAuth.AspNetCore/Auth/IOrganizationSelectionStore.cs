namespace Incursa.Integrations.WorkOS.AppAuth.Abstractions;

using Microsoft.AspNetCore.Http;

public interface IOrganizationSelectionStore
{
    string? Get(HttpContext context);

    void Set(HttpContext context, string organizationId);

    void Clear(HttpContext context);
}
