namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsTokenValidator
{
    Task<WorkOsTokenValidationResult> ValidateAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}
