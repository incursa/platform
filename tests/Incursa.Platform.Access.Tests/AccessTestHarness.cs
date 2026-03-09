#pragma warning disable MA0048
namespace Incursa.Platform.Access.Tests;

using Incursa.Platform.Access;
using Incursa.Platform.Access.Internal;
using Incursa.Platform.Access.WorkOS;
using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

internal sealed class AccessTestHarness : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    public AccessTestHarness(
        Action<AccessRegistryBuilder>? configureRegistry = null,
        Action<WorkOsAccessOptions>? configureWorkOs = null,
        DateTimeOffset? now = null)
    {
        FixedTimeProvider = new FixedTimeProvider(now ?? new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero));
        WorkItems = new InMemoryWorkStore<AccessWorkItem>();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(FixedTimeProvider);

        AddStorage(services, new InMemoryRecordStore<AccessUser>());
        AddStorage(services, new InMemoryRecordStore<ScopeRoot>());
        AddStorage(services, new InMemoryRecordStore<Tenant>());
        AddStorage(services, new InMemoryRecordStore<ScopeMembership>());
        AddStorage(services, new InMemoryRecordStore<AccessAssignment>());
        AddStorage(services, new InMemoryRecordStore<ExplicitPermissionGrant>());
        AddStorage(services, new InMemoryRecordStore<AccessAuditEntry>());
        AddStorage(services, new InMemoryLookupStore<MembershipByUserProjection>());
        AddStorage(services, new InMemoryLookupStore<MembershipByScopeRootProjection>());
        AddStorage(services, new InMemoryLookupStore<TenantByScopeRootProjection>());
        AddStorage(services, new InMemoryLookupStore<AccessibleTenantByUserProjection>());
        AddStorage(services, new InMemoryLookupStore<AssignmentByUserProjection>());
        AddStorage(services, new InMemoryLookupStore<AssignmentByResourceProjection>());
        AddStorage(services, new InMemoryLookupStore<GrantByUserProjection>());
        AddStorage(services, new InMemoryLookupStore<GrantByResourceProjection>());
        AddStorage(services, new InMemoryLookupStore<AuditEntryByUserProjection>());
        AddStorage(services, new InMemoryLookupStore<AuditEntryByResourceProjection>());
        AddStorage(services, new InMemoryLookupStore<ScopeRootByExternalLinkProjection>());
        AddStorage(services, new InMemoryLookupStore<TenantByExternalLinkProjection>());
        services.AddSingleton<IWorkStore<AccessWorkItem>>(WorkItems);
        services.AddSingleton<ICoordinationStore, InMemoryCoordinationStore>();

        var registryBuilder = new AccessRegistryBuilder();
        ConfigureRegistry(registryBuilder);
        configureRegistry?.Invoke(registryBuilder);

        services.AddAccess(registryBuilder.Build("test-v1"));
        services.AddWorkOsAccess(configureWorkOs);

        serviceProvider = services.BuildServiceProvider();
    }

    public FixedTimeProvider FixedTimeProvider { get; }

    public InMemoryWorkStore<AccessWorkItem> WorkItems { get; }

    public IAccessRegistry Registry => serviceProvider.GetRequiredService<IAccessRegistry>();

    public IAccessAdministrationService Administration => serviceProvider.GetRequiredService<IAccessAdministrationService>();

    public IAccessQueryService Query => serviceProvider.GetRequiredService<IAccessQueryService>();

    public IEffectiveAccessEvaluator Evaluator => serviceProvider.GetRequiredService<IEffectiveAccessEvaluator>();

    public IAccessAuditJournal AuditJournal => serviceProvider.GetRequiredService<IAccessAuditJournal>();

    public IWorkOsAccessSynchronizationService WorkOsSynchronization =>
        serviceProvider.GetRequiredService<IWorkOsAccessSynchronizationService>();

    public static AccessUser CreateUser(string id, string displayName = "Test User") =>
        new(new AccessUserId(id), displayName, email: id + "@example.test");

    public static ScopeRoot CreateOrganizationScopeRoot(
        string id,
        string displayName = "Contoso",
        IReadOnlyCollection<ExternalLink>? externalLinks = null) =>
        new(new ScopeRootId(id), ScopeRootKind.Organization, displayName, externalLinks: externalLinks);

    public static ScopeRoot CreatePersonalScopeRoot(string id, AccessUserId ownerUserId, string displayName = "Personal") =>
        new(new ScopeRootId(id), ScopeRootKind.Personal, displayName, ownerUserId);

    public static Tenant CreateTenant(string id, ScopeRootId scopeRootId, string displayName = "Tenant") =>
        new(new TenantId(id), scopeRootId, displayName);

    public static ExternalLink CreateWorkOsOrganizationLink(string organizationId) =>
        new(
            new ExternalLinkId("workos-org-link/" + Uri.EscapeDataString(organizationId)),
            WorkOsAccessDefaults.ProviderName,
            organizationId,
            WorkOsAccessDefaults.OrganizationResourceType);

    public static async Task<IReadOnlyCollection<TItem>> ToListAsync<TItem>(
        IAsyncEnumerable<TItem> source,
        CancellationToken cancellationToken = default)
    {
        List<TItem> items = [];
        await foreach (var item in source.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(item);
        }

        return items;
    }

    public void Dispose() => serviceProvider.Dispose();

    private static void AddStorage<TRecord>(IServiceCollection services, InMemoryRecordStore<TRecord> store)
        where TRecord : class =>
        services.AddSingleton<IRecordStore<TRecord>>(store);

    private static void AddStorage<TLookup>(IServiceCollection services, InMemoryLookupStore<TLookup> store)
        where TLookup : class =>
        services.AddSingleton<ILookupStore<TLookup>>(store);

    private static void ConfigureRegistry(AccessRegistryBuilder builder)
    {
        builder.AddPermission(new AccessPermissionDefinition(
            new AccessPermissionId("tenant.read"),
            "Read tenant",
            providerAliases: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WorkOsAccessDefaults.PermissionAliasKey] = "tenant.read",
            }));
        builder.AddPermission(new AccessPermissionDefinition(
            new AccessPermissionId("tenant.write"),
            "Write tenant"));
        builder.AddPermission(new AccessPermissionDefinition(
            new AccessPermissionId("resource.share"),
            "Share resource"));
        builder.AddRole(new AccessRoleDefinition(
            new AccessRoleId("organization-member"),
            "Organization member",
            [new AccessPermissionId("tenant.read")],
            providerAliases: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WorkOsAccessDefaults.RoleAliasKey] = "member",
            }));
        builder.AddRole(new AccessRoleDefinition(
            new AccessRoleId("organization-admin"),
            "Organization admin",
            [new AccessPermissionId("tenant.read"), new AccessPermissionId("tenant.write")],
            providerAliases: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WorkOsAccessDefaults.RoleAliasKey] = "admin",
            }));
        builder.AddRole(new AccessRoleDefinition(
            new AccessRoleId("tenant-reader"),
            "Tenant reader",
            [new AccessPermissionId("tenant.read")]));
        builder.AddRole(new AccessRoleDefinition(
            new AccessRoleId("tenant-editor"),
            "Tenant editor",
            [new AccessPermissionId("tenant.read"), new AccessPermissionId("tenant.write")]));
    }
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow) => this.utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;
}
#pragma warning restore MA0048
