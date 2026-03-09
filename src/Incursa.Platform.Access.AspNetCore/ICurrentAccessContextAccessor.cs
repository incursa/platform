namespace Incursa.Platform.Access.AspNetCore;

public interface ICurrentAccessContextAccessor
{
    ValueTask<CurrentAccessContext> GetCurrentAsync(CancellationToken cancellationToken = default);
}
