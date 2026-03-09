namespace Incursa.Integrations.WorkOS.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsRequestAuthContextAccessor
{
    WorkOsAuthIdentity? Current { get; }
}

public interface IWorkOsRequestAuthContextSetter
{
    void Set(WorkOsAuthIdentity? identity);
}

internal sealed class WorkOsRequestAuthContextAccessor : IWorkOsRequestAuthContextAccessor, IWorkOsRequestAuthContextSetter
{
    private WorkOsAuthIdentity? _current;

    public WorkOsAuthIdentity? Current => _current;

    public void Set(WorkOsAuthIdentity? identity)
    {
        _current = identity;
    }
}

