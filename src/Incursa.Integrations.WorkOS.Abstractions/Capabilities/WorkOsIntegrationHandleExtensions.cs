namespace Incursa.Integrations.WorkOS.Abstractions.Capabilities;

public static class WorkOsIntegrationHandleExtensions
{
    public static bool Supports<TCapability>(this IWorkOsIntegrationHandle handle)
        where TCapability : class
    {
        ArgumentNullException.ThrowIfNull(handle);
        return handle.TryGetCapability<TCapability>(out _);
    }

    public static bool TryGet<TCapability>(this IWorkOsIntegrationHandle handle, out TCapability? capability)
        where TCapability : class
    {
        ArgumentNullException.ThrowIfNull(handle);
        return handle.TryGetCapability(out capability);
    }

    public static TCapability GetRequired<TCapability>(this IWorkOsIntegrationHandle handle)
        where TCapability : class
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (handle.TryGetCapability<TCapability>(out var capability) && capability is not null)
        {
            return capability;
        }

        throw new CapabilityNotSupportedException(typeof(TCapability));
    }
}
