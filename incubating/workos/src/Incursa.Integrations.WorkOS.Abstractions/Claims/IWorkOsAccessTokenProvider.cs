namespace Incursa.Integrations.WorkOS.Abstractions.Claims;

public interface IWorkOsAccessTokenProvider
{
    ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default);
}
