namespace Incursa.Integrations.WorkOS.Abstractions.Widgets;

public interface IWorkOsCurrentSessionIdResolver
{
    Task<string?> ResolveAsync(CancellationToken cancellationToken);
}
