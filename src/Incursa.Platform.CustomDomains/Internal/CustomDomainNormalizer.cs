namespace Incursa.Platform.CustomDomains.Internal;

internal static class CustomDomainNormalizer
{
    public static CustomDomain Normalize(CustomDomain domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new CustomDomain(
            domain.Id,
            NormalizeHostname(domain.Hostname),
            domain.LifecycleStatus,
            domain.CertificateStatus,
            NormalizeNullable(domain.CertificateValidationMethod),
            NormalizeNullable(domain.LastError),
            NormalizeOwnershipVerification(domain.OwnershipVerification),
            domain.LastSynchronizedUtc,
            domain.ExternalLinks
                .Select(NormalizeExternalLink)
                .ToArray());
    }

    public static string NormalizeHostname(string hostname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        return hostname.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static CustomDomainExternalLink NormalizeExternalLink(CustomDomainExternalLink link)
    {
        ArgumentNullException.ThrowIfNull(link);

        return new CustomDomainExternalLink(
            link.Id,
            NormalizeRequired(link.Provider),
            NormalizeRequired(link.ExternalId),
            NormalizeNullable(link.ResourceType));
    }

    private static CustomDomainOwnershipVerification? NormalizeOwnershipVerification(CustomDomainOwnershipVerification? verification)
    {
        if (verification is null)
        {
            return null;
        }

        return new CustomDomainOwnershipVerification(
            NormalizeHostname(verification.Name),
            verification.RecordType,
            NormalizeRequired(verification.Value));
    }

    private static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
