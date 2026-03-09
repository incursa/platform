namespace Incursa.Platform.Access.Tests;

using Incursa.Platform.Access;
using Incursa.Platform.Access.WorkOS;

[Trait("Category", "Unit")]
public sealed class AccessRegistryAndEvaluationTests
{
    [Fact]
    public void Registry_ResolvesProviderAliasesWithoutParallelMappingRegistry()
    {
        using var harness = new AccessTestHarness();

        harness.Registry.TryGetRoleByProviderAlias(WorkOsAccessDefaults.RoleAliasKey, "admin", out var role).ShouldBeTrue();
        harness.Registry.TryGetPermissionByProviderAlias(WorkOsAccessDefaults.PermissionAliasKey, "tenant.read", out var permission)
            .ShouldBeTrue();

        role!.Id.ShouldBe(new AccessRoleId("organization-admin"));
        permission!.Id.ShouldBe(new AccessPermissionId("tenant.read"));
    }

    [Fact]
    public async Task EvaluateAsync_DeniesUserWithoutMembershipsAssignmentsOrGrants()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-1");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-1");
        var tenant = AccessTestHarness.CreateTenant("tenant-1", scopeRoot.Id);

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);

        var access = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(user.Id, AccessResourceReference.ForTenant(tenant.Id)),
            cancellationToken);

        access.IsAllowed.ShouldBeFalse();
        access.MembershipSatisfied.ShouldBeFalse();
        access.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_AllowsPersonalScopeOwnerWithoutMembership()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-2");
        var personalRoot = AccessTestHarness.CreatePersonalScopeRoot("personal-1", user.Id, "Samuel");
        var tenant = AccessTestHarness.CreateTenant("tenant-personal-1", personalRoot.Id, "Personal tenant");

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(personalRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);

        var access = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(user.Id, AccessResourceReference.ForTenant(tenant.Id)),
            cancellationToken);

        access.IsAllowed.ShouldBeTrue();
        access.MembershipSatisfied.ShouldBeTrue();
        access.Sources.Select(static item => item.Kind).ShouldContain(EffectiveAccessSourceKind.PersonalScopeOwner);
    }

    [Fact]
    public async Task EvaluateAsync_InheritsScopeRootRoleToTenantWhenConfigured()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-3");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-2");
        var tenant = AccessTestHarness.CreateTenant("tenant-2", scopeRoot.Id);

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);
        await harness.Administration.UpsertMembershipAsync(
            new ScopeMembership(new ScopeMembershipId("membership-1"), user.Id, scopeRoot.Id, harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);
        await harness.Administration.UpsertAssignmentAsync(
            new AccessAssignment(
                new AccessAssignmentId("assignment-1"),
                user.Id,
                new AccessRoleId("organization-admin"),
                AccessResourceReference.ForScopeRoot(scopeRoot.Id),
                harness.FixedTimeProvider.GetUtcNow(),
                AccessInheritanceMode.DescendantTenants),
            cancellationToken);

        var access = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(
                user.Id,
                AccessResourceReference.ForTenant(tenant.Id),
                [new AccessPermissionId("tenant.write")]),
            cancellationToken);

        access.IsAllowed.ShouldBeTrue();
        access.MembershipSatisfied.ShouldBeTrue();
        access.Permissions.ShouldContain(new AccessPermissionId("tenant.write"));
    }

    [Fact]
    public async Task EvaluateAsync_IsolatesDifferentTenantAssignments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-4");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-3");
        var tenantA = AccessTestHarness.CreateTenant("tenant-a", scopeRoot.Id, "Tenant A");
        var tenantB = AccessTestHarness.CreateTenant("tenant-b", scopeRoot.Id, "Tenant B");

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenantA, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenantB, cancellationToken);
        await harness.Administration.UpsertMembershipAsync(
            new ScopeMembership(new ScopeMembershipId("membership-2"), user.Id, scopeRoot.Id, harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);
        await harness.Administration.UpsertAssignmentAsync(
            new AccessAssignment(
                new AccessAssignmentId("assignment-tenant-a"),
                user.Id,
                new AccessRoleId("tenant-editor"),
                AccessResourceReference.ForTenant(tenantA.Id),
                harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);
        await harness.Administration.UpsertAssignmentAsync(
            new AccessAssignment(
                new AccessAssignmentId("assignment-tenant-b"),
                user.Id,
                new AccessRoleId("tenant-reader"),
                AccessResourceReference.ForTenant(tenantB.Id),
                harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);

        var tenantAAccess = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(
                user.Id,
                AccessResourceReference.ForTenant(tenantA.Id),
                [new AccessPermissionId("tenant.write")]),
            cancellationToken);
        var tenantBAccess = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(
                user.Id,
                AccessResourceReference.ForTenant(tenantB.Id),
                [new AccessPermissionId("tenant.write")]),
            cancellationToken);

        tenantAAccess.IsAllowed.ShouldBeTrue();
        tenantBAccess.IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_UsesExplicitGrantForSpecificResourceScope()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-5");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-4");
        var tenant = AccessTestHarness.CreateTenant("tenant-resource", scopeRoot.Id);
        var resource = AccessResourceReference.ForTenant(tenant.Id, "invoice-42");

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);
        await harness.Administration.UpsertExplicitPermissionGrantAsync(
            new ExplicitPermissionGrant(
                new ExplicitPermissionGrantId("grant-1"),
                user.Id,
                new AccessPermissionId("resource.share"),
                resource,
                harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);

        var granted = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(
                user.Id,
                resource,
                [new AccessPermissionId("resource.share")]),
            cancellationToken);
        var denied = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(
                user.Id,
                AccessResourceReference.ForTenant(tenant.Id, "invoice-43"),
                [new AccessPermissionId("resource.share")]),
            cancellationToken);

        granted.IsAllowed.ShouldBeTrue();
        denied.IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_AllowsDirectTenantAssignmentWithoutMembership()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-6");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-5");
        var tenant = AccessTestHarness.CreateTenant("tenant-direct", scopeRoot.Id);

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);
        await harness.Administration.UpsertAssignmentAsync(
            new AccessAssignment(
                new AccessAssignmentId("assignment-direct-1"),
                user.Id,
                new AccessRoleId("tenant-reader"),
                AccessResourceReference.ForTenant(tenant.Id),
                harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);

        var access = await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(
                user.Id,
                AccessResourceReference.ForTenant(tenant.Id),
                [new AccessPermissionId("tenant.read")]),
            cancellationToken);

        access.IsAllowed.ShouldBeTrue();
        access.MembershipSatisfied.ShouldBeFalse();
    }
}
