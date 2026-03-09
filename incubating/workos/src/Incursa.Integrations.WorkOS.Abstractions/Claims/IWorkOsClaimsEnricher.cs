namespace Incursa.Integrations.WorkOS.Abstractions.Claims;

using System.Security.Claims;

public interface IWorkOsClaimsEnricher
{
    ValueTask EnrichAsync(
        ClaimsPrincipal principal,
        ClaimsIdentity identity,
        CancellationToken ct = default);
}
