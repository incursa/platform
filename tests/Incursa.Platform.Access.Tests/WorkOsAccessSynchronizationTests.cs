namespace Incursa.Platform.Access.Tests;

using Incursa.Platform.Access;
using Incursa.Platform.Access.WorkOS;

[Trait("Category", "Unit")]
public sealed class WorkOsAccessSynchronizationTests
{
    [Fact]
    public async Task SynchronizeAsync_CreatesScopeRootMembershipAndAssignmentsFromProviderAliases()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-20");

        var result = await harness.WorkOsSynchronization.SynchronizeAsync(
            new WorkOsAccessSyncRequest(
                user,
                [
                    new WorkOsOrganizationMembership("org_123", ["admin", "unknown"], "Contoso"),
                ]),
            cancellationToken);

        result.ScopeRoots.ShouldHaveSingleItem().DisplayName.ShouldBe("Contoso");
        result.Memberships.ShouldHaveSingleItem().ScopeRootId.ShouldBe(result.ScopeRoots.Single().Id);
        result.Assignments.Select(static item => item.RoleId).ShouldContain(new AccessRoleId("organization-admin"));
        result.UnmappedRoleSlugs.ShouldContain("unknown");

        var scopeRoot = await harness.Query.GetScopeRootByExternalLinkAsync(
            WorkOsAccessDefaults.ProviderName,
            "org_123",
            WorkOsAccessDefaults.OrganizationResourceType,
            cancellationToken);

        scopeRoot.ShouldNotBeNull();
    }

    [Fact]
    public async Task SynchronizeAsync_ReconcilesProviderManagedAssignmentsWithoutTouchingManualAssignments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-21");
        var scopeRoot = AccessTestHarness.CreateOrganizationScopeRoot(
            "scope-root-21",
            externalLinks: [AccessTestHarness.CreateWorkOsOrganizationLink("org_21")]);

        await harness.Administration.UpsertUserAsync(user, cancellationToken);
        await harness.Administration.UpsertScopeRootAsync(scopeRoot, cancellationToken);
        await harness.Administration.UpsertAssignmentAsync(
            new AccessAssignment(
                new AccessAssignmentId("manual-assignment-21"),
                user.Id,
                new AccessRoleId("tenant-reader"),
                AccessResourceReference.ForScopeRoot(scopeRoot.Id),
                harness.FixedTimeProvider.GetUtcNow()),
            cancellationToken);

        await harness.WorkOsSynchronization.SynchronizeAsync(
            new WorkOsAccessSyncRequest(
                user,
                [new WorkOsOrganizationMembership("org_21", ["admin", "member"])]),
            cancellationToken);

        var result = await harness.WorkOsSynchronization.SynchronizeAsync(
            new WorkOsAccessSyncRequest(
                user,
                [new WorkOsOrganizationMembership("org_21", ["member"])]),
            cancellationToken);

        result.DeletedAssignmentIds.Select(static item => item.Value)
            .ShouldContain(static value => value.StartsWith("workos-assignment:", StringComparison.Ordinal));

        var assignments = await AccessTestHarness.ToListAsync(
            harness.Query.GetAssignmentsForUserAsync(user.Id, cancellationToken),
            cancellationToken);

        assignments.Select(static item => item.Id).ShouldContain(new AccessAssignmentId("manual-assignment-21"));
        assignments.Select(static item => item.RoleId).ShouldContain(new AccessRoleId("organization-member"));
        assignments.Select(static item => item.RoleId).ShouldNotContain(new AccessRoleId("organization-admin"));
    }

    [Fact]
    public async Task SynchronizeAsync_DeletesRemovedMemberships()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-22");

        await harness.WorkOsSynchronization.SynchronizeAsync(
            new WorkOsAccessSyncRequest(
                user,
                [
                    new WorkOsOrganizationMembership("org_22_a", ["member"]),
                    new WorkOsOrganizationMembership("org_22_b", ["member"]),
                ]),
            cancellationToken);

        var result = await harness.WorkOsSynchronization.SynchronizeAsync(
            new WorkOsAccessSyncRequest(
                user,
                [new WorkOsOrganizationMembership("org_22_a", ["member"])]),
            cancellationToken);

        result.DeletedMembershipIds.ShouldHaveSingleItem();

        var memberships = await AccessTestHarness.ToListAsync(
            harness.Query.GetMembershipsForUserAsync(user.Id, cancellationToken),
            cancellationToken);
        memberships.Select(static item => item.Id).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SynchronizeAsync_EnqueuesRequestedReconciliationWorkItem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new AccessTestHarness();
        var user = AccessTestHarness.CreateUser("user-23");

        var result = await harness.WorkOsSynchronization.SynchronizeAsync(
            new WorkOsAccessSyncRequest(
                user,
                [new WorkOsOrganizationMembership("org_23", ["member"])],
                enqueueReconciliationWorkItem: true,
                reconciliationDetail: "workos webhook refresh"),
            cancellationToken);

        result.ReconciliationWorkItem.ShouldNotBeNull();
        harness.WorkItems.EnqueuedItems.ShouldContain(
            item => item.Payload.Id == result.ReconciliationWorkItem!.Id
                && item.Payload.Kind == AccessWorkItemKind.Reconciliation);
    }
}
