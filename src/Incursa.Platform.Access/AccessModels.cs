#pragma warning disable MA0048
namespace Incursa.Platform.Access;

public enum ScopeRootKind
{
    Organization = 0,
    Personal = 1,
}

public enum AccessResourceKind
{
    ScopeRoot = 0,
    Tenant = 1,
}

public enum AccessInheritanceMode
{
    None = 0,
    DescendantTenants = 1,
}

public enum EffectiveAccessSourceKind
{
    RoleAssignment = 0,
    ExplicitPermissionGrant = 1,
    PersonalScopeOwner = 2,
    ScopeMembership = 3,
}

public enum AccessWorkItemKind
{
    ProjectionRebuild = 0,
    ProviderSync = 1,
    Reconciliation = 2,
}

public sealed record ExternalLink
{
    public ExternalLink(
        ExternalLinkId id,
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

    public ExternalLinkId Id { get; }

    public string Provider { get; }

    public string ExternalId { get; }

    public string? ResourceType { get; }
}

public sealed record AccessUser
{
    public AccessUser(
        AccessUserId id,
        string displayName,
        string? email = null,
        DateTimeOffset? createdUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id;
        DisplayName = displayName.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        CreatedUtc = createdUtc;
    }

    public AccessUserId Id { get; }

    public string DisplayName { get; }

    public string? Email { get; }

    public DateTimeOffset? CreatedUtc { get; }
}

public sealed record ScopeRoot
{
    public ScopeRoot(
        ScopeRootId id,
        ScopeRootKind kind,
        string displayName,
        AccessUserId? ownerUserId = null,
        DateTimeOffset? createdUtc = null,
        IReadOnlyCollection<ExternalLink>? externalLinks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (kind == ScopeRootKind.Personal && ownerUserId is null)
        {
            throw new ArgumentException("Personal scope roots require an owner user.", nameof(ownerUserId));
        }

        Id = id;
        Kind = kind;
        DisplayName = displayName.Trim();
        OwnerUserId = ownerUserId;
        CreatedUtc = createdUtc;
        ExternalLinks = externalLinks is null ? Array.Empty<ExternalLink>() : externalLinks.ToArray();
    }

    public ScopeRootId Id { get; }

    public ScopeRootKind Kind { get; }

    public string DisplayName { get; }

    public AccessUserId? OwnerUserId { get; }

    public DateTimeOffset? CreatedUtc { get; }

    public IReadOnlyCollection<ExternalLink> ExternalLinks { get; }
}

public sealed record Tenant
{
    public Tenant(
        TenantId id,
        ScopeRootId scopeRootId,
        string displayName,
        DateTimeOffset? createdUtc = null,
        IReadOnlyCollection<ExternalLink>? externalLinks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id;
        ScopeRootId = scopeRootId;
        DisplayName = displayName.Trim();
        CreatedUtc = createdUtc;
        ExternalLinks = externalLinks is null ? Array.Empty<ExternalLink>() : externalLinks.ToArray();
    }

    public TenantId Id { get; }

    public ScopeRootId ScopeRootId { get; }

    public string DisplayName { get; }

    public DateTimeOffset? CreatedUtc { get; }

    public IReadOnlyCollection<ExternalLink> ExternalLinks { get; }
}

public sealed record ScopeMembership
{
    public ScopeMembership(
        ScopeMembershipId id,
        AccessUserId userId,
        ScopeRootId scopeRootId,
        DateTimeOffset createdUtc,
        bool isActive = true)
    {
        Id = id;
        UserId = userId;
        ScopeRootId = scopeRootId;
        CreatedUtc = createdUtc;
        IsActive = isActive;
    }

    public ScopeMembershipId Id { get; }

    public AccessUserId UserId { get; }

    public ScopeRootId ScopeRootId { get; }

    public DateTimeOffset CreatedUtc { get; }

    public bool IsActive { get; }
}

public sealed record AccessResourceReference
{
    private AccessResourceReference(AccessResourceKind kind, ScopeRootId? scopeRootId, TenantId? tenantId, string? resourceId)
    {
        Kind = kind;
        ScopeRootId = scopeRootId;
        TenantId = tenantId;
        ResourceId = string.IsNullOrWhiteSpace(resourceId) ? null : resourceId.Trim();
    }

    public AccessResourceKind Kind { get; }

    public ScopeRootId? ScopeRootId { get; }

    public TenantId? TenantId { get; }

    public string? ResourceId { get; }

    public static AccessResourceReference ForScopeRoot(ScopeRootId scopeRootId, string? resourceId = null) =>
        new(AccessResourceKind.ScopeRoot, scopeRootId, null, resourceId);

    public static AccessResourceReference ForTenant(TenantId tenantId, string? resourceId = null) =>
        new(AccessResourceKind.Tenant, null, tenantId, resourceId);

    public override string ToString()
    {
        var resourceSuffix = string.IsNullOrWhiteSpace(ResourceId) ? string.Empty : "/resource/" + ResourceId;
        return Kind switch
        {
            AccessResourceKind.ScopeRoot => "scope-root/" + ScopeRootId + resourceSuffix,
            AccessResourceKind.Tenant => "tenant/" + TenantId + resourceSuffix,
            _ => throw new InvalidOperationException("Unknown access resource kind."),
        };
    }
}

public sealed record AccessAssignment
{
    public AccessAssignment(
        AccessAssignmentId id,
        AccessUserId userId,
        AccessRoleId roleId,
        AccessResourceReference resource,
        DateTimeOffset createdUtc,
        AccessInheritanceMode inheritanceMode = AccessInheritanceMode.None)
    {
        ArgumentNullException.ThrowIfNull(resource);

        Id = id;
        UserId = userId;
        RoleId = roleId;
        Resource = resource;
        CreatedUtc = createdUtc;
        InheritanceMode = inheritanceMode;
    }

    public AccessAssignmentId Id { get; }

    public AccessUserId UserId { get; }

    public AccessRoleId RoleId { get; }

    public AccessResourceReference Resource { get; }

    public DateTimeOffset CreatedUtc { get; }

    public AccessInheritanceMode InheritanceMode { get; }
}

public sealed record ExplicitPermissionGrant
{
    public ExplicitPermissionGrant(
        ExplicitPermissionGrantId id,
        AccessUserId userId,
        AccessPermissionId permissionId,
        AccessResourceReference resource,
        DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (string.IsNullOrWhiteSpace(resource.ResourceId))
        {
            throw new ArgumentException("Explicit permission grants require a specific resource identifier.", nameof(resource));
        }

        Id = id;
        UserId = userId;
        PermissionId = permissionId;
        Resource = resource;
        CreatedUtc = createdUtc;
    }

    public ExplicitPermissionGrantId Id { get; }

    public AccessUserId UserId { get; }

    public AccessPermissionId PermissionId { get; }

    public AccessResourceReference Resource { get; }

    public DateTimeOffset CreatedUtc { get; }
}

public sealed record EffectiveAccessSource
{
    public EffectiveAccessSource(EffectiveAccessSourceKind kind, string sourceId, string? detail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        Kind = kind;
        SourceId = sourceId.Trim();
        Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
    }

    public EffectiveAccessSourceKind Kind { get; }

    public string SourceId { get; }

    public string? Detail { get; }
}

public sealed record EffectiveAccess
{
    public EffectiveAccess(
        AccessUserId userId,
        AccessResourceReference resource,
        bool isAllowed,
        bool membershipSatisfied,
        ScopeRootId? scopeRootId,
        IReadOnlyCollection<AccessPermissionId>? permissions = null,
        IReadOnlyCollection<EffectiveAccessSource>? sources = null)
    {
        ArgumentNullException.ThrowIfNull(resource);

        UserId = userId;
        Resource = resource;
        IsAllowed = isAllowed;
        MembershipSatisfied = membershipSatisfied;
        ScopeRootId = scopeRootId;
        Permissions = permissions is null ? Array.Empty<AccessPermissionId>() : permissions.ToArray();
        Sources = sources is null ? Array.Empty<EffectiveAccessSource>() : sources.ToArray();
    }

    public AccessUserId UserId { get; }

    public AccessResourceReference Resource { get; }

    public bool IsAllowed { get; }

    public bool MembershipSatisfied { get; }

    public ScopeRootId? ScopeRootId { get; }

    public IReadOnlyCollection<AccessPermissionId> Permissions { get; }

    public IReadOnlyCollection<EffectiveAccessSource> Sources { get; }
}

public sealed record AccessAuditEntry
{
    public AccessAuditEntry(
        AccessAuditEntryId id,
        DateTimeOffset occurredUtc,
        string action,
        AccessUserId? actorUserId = null,
        AccessUserId? subjectUserId = null,
        AccessResourceReference? resource = null,
        string? summary = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        Id = id;
        OccurredUtc = occurredUtc;
        Action = action.Trim();
        ActorUserId = actorUserId;
        SubjectUserId = subjectUserId;
        Resource = resource;
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
    }

    public AccessAuditEntryId Id { get; }

    public DateTimeOffset OccurredUtc { get; }

    public string Action { get; }

    public AccessUserId? ActorUserId { get; }

    public AccessUserId? SubjectUserId { get; }

    public AccessResourceReference? Resource { get; }

    public string? Summary { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}

public sealed record AccessWorkItem
{
    public AccessWorkItem(string id, AccessWorkItemKind kind, string subject, string? detail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Id = id.Trim();
        Kind = kind;
        Subject = subject.Trim();
        Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
    }

    public string Id { get; }

    public AccessWorkItemKind Kind { get; }

    public string Subject { get; }

    public string? Detail { get; }
}

public sealed record AccessPermissionDefinition
{
    public AccessPermissionDefinition(
        AccessPermissionId id,
        string displayName,
        string? description = null,
        IReadOnlyDictionary<string, string>? providerAliases = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id;
        DisplayName = displayName.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ProviderAliases = providerAliases is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(providerAliases, StringComparer.Ordinal);
    }

    public AccessPermissionId Id { get; }

    public string DisplayName { get; }

    public string? Description { get; }

    public IReadOnlyDictionary<string, string> ProviderAliases { get; }
}

public sealed record AccessRoleDefinition
{
    public AccessRoleDefinition(
        AccessRoleId id,
        string displayName,
        IReadOnlyCollection<AccessPermissionId> permissions,
        string? description = null,
        IReadOnlyDictionary<string, string>? providerAliases = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(permissions);

        Id = id;
        DisplayName = displayName.Trim();
        Permissions = permissions.ToArray();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ProviderAliases = providerAliases is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(providerAliases, StringComparer.Ordinal);
    }

    public AccessRoleId Id { get; }

    public string DisplayName { get; }

    public IReadOnlyCollection<AccessPermissionId> Permissions { get; }

    public string? Description { get; }

    public IReadOnlyDictionary<string, string> ProviderAliases { get; }
}

public sealed record AccessRegistrySnapshot(
    string Version,
    IReadOnlyCollection<AccessPermissionDefinition> Permissions,
    IReadOnlyCollection<AccessRoleDefinition> Roles);

public sealed record AccessEvaluationRequest(
    AccessUserId UserId,
    AccessResourceReference Resource,
    IReadOnlyCollection<AccessPermissionId>? requestedPermissions = null)
{
    public IReadOnlyCollection<AccessPermissionId> RequiredPermissions { get; } = requestedPermissions?.ToArray() ?? Array.Empty<AccessPermissionId>();
}
#pragma warning restore MA0048
