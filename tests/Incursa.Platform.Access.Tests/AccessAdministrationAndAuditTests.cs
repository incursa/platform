namespace Incursa.Platform.Access.Tests;

using Incursa.Platform.Access;

[Trait("Category", "Unit")]
public sealed class AccessAdministrationAndAuditTests
{
    [Fact]
    public async Task Administration_ProjectsAccessibleTenantWhenMembershipAndAssignmentExist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-10");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-10");
        var tenant = AccessTestHarness.CreateTenant("tenant-10", scopeRoot.Id);

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);
        await harness.Administration.UpsertMembershipAsync(
            new ScopeMembership(new ScopeMembershipId("membership-10"), user.Id, scopeRoot.Id, harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);
        await harness.Administration.UpsertAssignmentAsync(
            new AccessAssignment(
                new AccessAssignmentId("assignment-10"),
                user.Id,
                new AccessRoleId("organization-member"),
                AccessResourceReference.ForScopeRoot(scopeRoot.Id),
                harness.FixedTimeProvider.GetUtcNow(),
                AccessInheritanceMode.DescendantTenants),
            cancellationToken);

        var accessibleTenants = await AccessTestHarness.ToListAsync(
            harness.Query.GetAccessibleTenantsAsync(user.Id, cancellationToken),
            cancellationToken);

        accessibleTenants.Select(static item => item.Id).ShouldContain(tenant.Id);
    }

    [Fact]
    public async Task Administration_RemovesAccessibleTenantProjectionWhenAssignmentDeleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-11");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-11");
        var tenant = AccessTestHarness.CreateTenant("tenant-11", scopeRoot.Id);
        var assignmentId = new AccessAssignmentId("assignment-11");

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);
        await harness.Administration.UpsertMembershipAsync(
            new ScopeMembership(new ScopeMembershipId("membership-11"), user.Id, scopeRoot.Id, harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);
        await harness.Administration.UpsertAssignmentAsync(
            new AccessAssignment(
                assignmentId,
                user.Id,
                new AccessRoleId("organization-member"),
                AccessResourceReference.ForScopeRoot(scopeRoot.Id),
                harness.FixedTimeProvider.GetUtcNow(),
                AccessInheritanceMode.DescendantTenants),
            cancellationToken);

        (await AccessTestHarness.ToListAsync(
            harness.Query.GetAccessibleTenantsAsync(user.Id, cancellationToken),
            cancellationToken)).ShouldNotBeEmpty();

        (await harness.Administration.DeleteAssignmentAsync(assignmentId, cancellationToken)).ShouldBeTrue();

        (await AccessTestHarness.ToListAsync(
            harness.Query.GetAccessibleTenantsAsync(user.Id, cancellationToken),
            cancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Administration_DeletesExplicitGrantAndRevokesResourceAccess()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-12");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot("scope-root-12");
        var tenant = AccessTestHarness.CreateTenant("tenant-12", scopeRoot.Id);
        var grantId = new ExplicitPermissionGrantId("grant-12");
        var resource = AccessResourceReference.ForTenant(tenant.Id, "resource-12");

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertTenantAsync(tenant, cancellationToken);
        await harness.Administration.UpsertExplicitPermissionGrantAsync(
            new ExplicitPermissionGrant(
                grantId,
                user.Id,
                new AccessPermissionId("resource.share"),
                resource,
                harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);

        (await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(user.Id, resource, [new AccessPermissionId("resource.share")]),
            cancellationToken)).IsAllowed.ShouldBeTrue();

        (await harness.Administration.DeleteExplicitPermissionGrantAsync(grantId, cancellationToken)).ShouldBeTrue();

        (await harness.Evaluator.EvaluateAsync(
            new AccessEvaluationRequest(user.Id, resource, [new AccessPermissionId("resource.share")]),
            cancellationToken)).IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task AuditJournal_AppendsAndQueriesByUserAndResource()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-13");
        var resource = AccessResourceReference.ForTenant(new TenantId("tenant-13"), "resource-13");
        var entry = new AccessAuditEntry(
            new AccessAuditEntryId("audit-13"),
            harness.FixedTimeProvider.GetUtcNow(),
            "access.permission-grant.upserted",
            subjectUserId: user.Id,
            resource: resource,
            summary: "permission granted");

        await harness.AuditJournal.AppendAsync(entry, cancellationToken);

        var byUser = await AccessTestHarness.ToListAsync(
            harness.AuditJournal.QueryByUserAsync(user.Id, cancellationToken: cancellationToken),
            cancellationToken);
        var byResource = await AccessTestHarness.ToListAsync(
            harness.AuditJournal.QueryByResourceAsync(resource, cancellationToken: cancellationToken),
            cancellationToken);

        byUser.ShouldContain(entry);
        byResource.ShouldContain(entry);
    }
}
