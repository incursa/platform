namespace Incursa.Integrations.WorkOS.Abstractions.Capabilities;

public interface IWorkOsIntegrationHandle
{
    bool TryGetCapability<TCapability>(out TCapability? capability)
        where TCapability : class;
}
