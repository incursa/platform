#pragma warning disable MA0048
namespace Incursa.Platform.Access.Internal;

internal sealed record MembershipByUserProjection(ScopeMembership Membership);

internal sealed record MembershipByScopeRootProjection(ScopeMembership Membership);

internal sealed record TenantByScopeRootProjection(Tenant Tenant);

internal sealed record AccessibleTenantByUserProjection(Tenant Tenant);

internal sealed record AssignmentByUserProjection(AccessAssignment Assignment);

internal sealed record AssignmentByResourceProjection(AccessAssignment Assignment);

internal sealed record GrantByUserProjection(ExplicitPermissionGrant Grant);

internal sealed record GrantByResourceProjection(ExplicitPermissionGrant Grant);

internal sealed record AuditEntryByUserProjection(AccessAuditEntry Entry);

internal sealed record AuditEntryByResourceProjection(AccessAuditEntry Entry);

internal sealed record ScopeRootByExternalLinkProjection(ScopeRoot ScopeRoot);

internal sealed record TenantByExternalLinkProjection(Tenant Tenant);
#pragma warning restore MA0048
