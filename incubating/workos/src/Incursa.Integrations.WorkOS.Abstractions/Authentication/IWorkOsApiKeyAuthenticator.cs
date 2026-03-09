namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsApiKeyAuthenticator
{
    ValueTask<WorkOsApiKeyValidationResult> ValidateApiKeyAsync(string presentedKey, CancellationToken ct = default);
}

