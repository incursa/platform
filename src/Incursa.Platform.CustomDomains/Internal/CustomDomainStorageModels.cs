#pragma warning disable MA0048
namespace Incursa.Platform.CustomDomains.Internal;

internal sealed record DomainByHostnameProjection(CustomDomain Domain);

internal sealed record DomainByExternalLinkProjection(CustomDomain Domain);
#pragma warning restore MA0048
