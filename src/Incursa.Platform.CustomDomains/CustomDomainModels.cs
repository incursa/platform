#pragma warning disable MA0048
namespace Incursa.Platform.CustomDomains;

public enum CustomDomainLifecycleStatus
{
    Unknown = 0,
    Pending = 1,
    Active = 2,
    Failed = 3,
    Removed = 4,
}

public enum CustomDomainCertificateStatus
{
    Unknown = 0,
    Pending = 1,
    Active = 2,
    Failed = 3,
}

public enum CustomDomainVerificationRecordType
{
    Txt = 0,
    CName = 1,
    Http = 2,
    Other = 3,
}

public sealed record CustomDomainExternalLink
{
    public CustomDomainExternalLink(
        CustomDomainExternalLinkId id,
        string provider,
        string externalId,
        string? resourceType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        Id = id;
        Provider = provider.Trim();
        ExternalId = externalId.Trim();
        ResourceType = string.IsNullOrWhiteSpace(resourceType) ? null : resourceType.Trim();
    }

    public CustomDomainExternalLinkId Id { get; }

    public string Provider { get; }

    public string ExternalId { get; }

    public string? ResourceType { get; }
}

public sealed record CustomDomainOwnershipVerification
{
    public CustomDomainOwnershipVerification(
        string name,
        CustomDomainVerificationRecordType recordType,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Name = name.Trim();
        RecordType = recordType;
        Value = value.Trim();
    }

    public string Name { get; }

    public CustomDomainVerificationRecordType RecordType { get; }

    public string Value { get; }
}

public sealed record CustomDomain
{
    public CustomDomain(
        CustomDomainId id,
        string hostname,
        CustomDomainLifecycleStatus lifecycleStatus = CustomDomainLifecycleStatus.Unknown,
        CustomDomainCertificateStatus certificateStatus = CustomDomainCertificateStatus.Unknown,
        string? certificateValidationMethod = null,
        string? lastError = null,
        CustomDomainOwnershipVerification? ownershipVerification = null,
        DateTimeOffset? lastSynchronizedUtc = null,
        IReadOnlyCollection<CustomDomainExternalLink>? externalLinks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);

        Id = id;
        Hostname = hostname.Trim();
        LifecycleStatus = lifecycleStatus;
        CertificateStatus = certificateStatus;
        CertificateValidationMethod = string.IsNullOrWhiteSpace(certificateValidationMethod)
            ? null
            : certificateValidationMethod.Trim();
        LastError = string.IsNullOrWhiteSpace(lastError) ? null : lastError.Trim();
        OwnershipVerification = ownershipVerification;
        LastSynchronizedUtc = lastSynchronizedUtc;
        ExternalLinks = externalLinks is null ? Array.Empty<CustomDomainExternalLink>() : externalLinks.ToArray();
    }

    public CustomDomainId Id { get; }

    public string Hostname { get; }

    public CustomDomainLifecycleStatus LifecycleStatus { get; }

    public CustomDomainCertificateStatus CertificateStatus { get; }

    public string? CertificateValidationMethod { get; }

    public string? LastError { get; }

    public CustomDomainOwnershipVerification? OwnershipVerification { get; }

    public DateTimeOffset? LastSynchronizedUtc { get; }

    public IReadOnlyCollection<CustomDomainExternalLink> ExternalLinks { get; }
}
#pragma warning restore MA0048
