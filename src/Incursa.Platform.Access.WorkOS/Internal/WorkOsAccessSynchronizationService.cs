namespace Incursa.Platform.Access.WorkOS;

using System.Globalization;
using Microsoft.Extensions.Options;

internal sealed class WorkOsAccessSynchronizationService : IWorkOsAccessSynchronizationService
{
    private const string ScopeRootIdPrefix = "workos-scope-root";
    private const string ScopeRootLinkIdPrefix = "workos-scope-root-link";
    private const string MembershipIdPrefix = "workos-membership";
    private const string AssignmentIdPrefix = "workos-assignment";
    private readonly IAccessAdministrationService administrationService;
    private readonly IAccessQueryService queryService;
    private readonly IAccessRegistry registry;
    private readonly TimeProvider timeProvider;
    private readonly WorkOsAccessOptions options;

    public WorkOsAccessSynchronizationService(
        IAccessAdministrationService administrationService,
        IAccessQueryService queryService,
        IAccessRegistry registry,
        IOptions<WorkOsAccessOptions> options,
        TimeProvider timeProvider)
    {
        this.administrationService = administrationService ?? throw new ArgumentNullException(nameof(administrationService));
        this.queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<WorkOsAccessSyncResult> SynchronizeAsync(
        WorkOsAccessSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateOptions();

        AccessUser user = await administrationService.UpsertUserAsync(request.User, cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = timeProvider.GetUtcNow();

        Dictionary<string, WorkOsOrganizationMembership> membershipsByOrganization = request.Memberships
            .GroupBy(static item => item.OrganizationId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => new WorkOsOrganizationMembership(
                    group.Key,
                    group.SelectMany(static item => item.RoleSlugs).Distinct(StringComparer.Ordinal).ToArray(),
                    group.Select(static item => item.OrganizationDisplayName).FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item))),
                StringComparer.Ordinal);

        List<ScopeRoot> synchronizedScopeRoots = [];
        List<ScopeMembership> synchronizedMemberships = [];
        Dictionary<AccessAssignmentId, AccessAssignment> synchronizedAssignments = [];
        HashSet<string> unmappedRoleSlugs = new(StringComparer.Ordinal);
        HashSet<ScopeMembershipId> expectedMembershipIds = [];
        HashSet<AccessAssignmentId> expectedAssignmentIds = [];

        foreach ((string organizationId, WorkOsOrganizationMembership membership) in membershipsByOrganization)
        {
            ScopeRoot scopeRoot = await GetOrCreateScopeRootAsync(membership, now, cancellationToken).ConfigureAwait(false);
            synchronizedScopeRoots.Add(scopeRoot);

            ScopeMembership upsertedMembership = await UpsertMembershipAsync(
                user.Id,
                organizationId,
                scopeRoot.Id,
                now,
                cancellationToken).ConfigureAwait(false);

            synchronizedMemberships.Add(upsertedMembership);
            expectedMembershipIds.Add(upsertedMembership.Id);

            foreach (string roleSlug in membership.RoleSlugs)
            {
                if (!registry.TryGetRoleByProviderAlias(options.RoleAliasKey, roleSlug, out var role))
                {
                    unmappedRoleSlugs.Add(roleSlug);
                    continue;
                }

                AccessAssignment assignment = await UpsertAssignmentAsync(
                    user.Id,
                    organizationId,
                    scopeRoot.Id,
                    role.Id,
                    now,
                    cancellationToken).ConfigureAwait(false);

                synchronizedAssignments[assignment.Id] = assignment;
                expectedAssignmentIds.Add(assignment.Id);
            }
        }

        IReadOnlyCollection<AccessAssignmentId> deletedAssignmentIds = await DeleteStaleAssignmentsAsync(
            user.Id,
            expectedAssignmentIds,
            cancellationToken).ConfigureAwait(false);
        IReadOnlyCollection<ScopeMembershipId> deletedMembershipIds = await DeleteStaleMembershipsAsync(
            user.Id,
            expectedMembershipIds,
            cancellationToken).ConfigureAwait(false);

        AccessWorkItem? reconciliationWorkItem = null;
        if (request.EnqueueReconciliationWorkItem)
        {
            reconciliationWorkItem = new AccessWorkItem(
                MakeUniqueId("workos-reconciliation", user.Id.Value, now),
                AccessWorkItemKind.Reconciliation,
                user.Id.Value,
                request.ReconciliationDetail ?? "workos access synchronization reconciliation");

            await administrationService.EnqueueWorkItemAsync(reconciliationWorkItem, cancellationToken).ConfigureAwait(false);
        }

        return new WorkOsAccessSyncResult(
            user,
            synchronizedScopeRoots.OrderBy(static item => item.Id.Value, StringComparer.Ordinal).ToArray(),
            synchronizedMemberships.OrderBy(static item => item.ScopeRootId.Value, StringComparer.Ordinal).ToArray(),
            synchronizedAssignments.Values.OrderBy(static item => item.RoleId.Value, StringComparer.Ordinal).ToArray(),
            deletedMembershipIds,
            deletedAssignmentIds,
            unmappedRoleSlugs.OrderBy(static item => item, StringComparer.Ordinal).ToArray(),
            reconciliationWorkItem);
    }

    private async Task<ScopeRoot> GetOrCreateScopeRootAsync(
        WorkOsOrganizationMembership membership,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ScopeRoot? existingScopeRoot = await queryService.GetScopeRootByExternalLinkAsync(
            options.ProviderName,
            membership.OrganizationId,
            options.OrganizationResourceType,
            cancellationToken).ConfigureAwait(false);
        if (existingScopeRoot is not null && existingScopeRoot.Kind != ScopeRootKind.Organization)
        {
            throw new InvalidOperationException(
                $"WorkOS organization '{membership.OrganizationId}' is linked to non-organization scope root '{existingScopeRoot.Id}'.");
        }

        if (existingScopeRoot is null && !options.CreateMissingScopeRoots)
        {
            throw new InvalidOperationException(
                $"WorkOS organization '{membership.OrganizationId}' is not linked to a local scope root and automatic creation is disabled.");
        }

        ScopeRoot scopeRoot = new(
            existingScopeRoot?.Id ?? new ScopeRootId(MakeStableId(ScopeRootIdPrefix, membership.OrganizationId)),
            ScopeRootKind.Organization,
            membership.OrganizationDisplayName ?? existingScopeRoot?.DisplayName ?? membership.OrganizationId,
            ownerUserId: existingScopeRoot?.OwnerUserId,
            createdUtc: existingScopeRoot?.CreatedUtc ?? now,
            externalLinks: MergeExternalLinks(existingScopeRoot?.ExternalLinks, membership.OrganizationId));

        return await administrationService.UpsertScopeRootAsync(scopeRoot, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ScopeMembership> UpsertMembershipAsync(
        AccessUserId userId,
        string organizationId,
        ScopeRootId scopeRootId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ScopeMembership membership = new(
            new ScopeMembershipId(MakeStableId(MembershipIdPrefix, userId.Value, organizationId)),
            userId,
            scopeRootId,
            now);

        return await administrationService.UpsertMembershipAsync(membership, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AccessAssignment> UpsertAssignmentAsync(
        AccessUserId userId,
        string organizationId,
        ScopeRootId scopeRootId,
        AccessRoleId roleId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        AccessAssignment assignment = new(
            new AccessAssignmentId(MakeStableId(AssignmentIdPrefix, userId.Value, organizationId, roleId.Value)),
            userId,
            roleId,
            AccessResourceReference.ForScopeRoot(scopeRootId),
            now,
            options.AssignmentInheritanceMode);

        return await administrationService.UpsertAssignmentAsync(assignment, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyCollection<ScopeMembershipId>> DeleteStaleMembershipsAsync(
        AccessUserId userId,
        IReadOnlySet<ScopeMembershipId> expectedMembershipIds,
        CancellationToken cancellationToken)
    {
        if (!options.ReconcileMissingMemberships)
        {
            return Array.Empty<ScopeMembershipId>();
        }

        List<ScopeMembershipId> deletedMembershipIds = [];
        await foreach (var membership in queryService.GetMembershipsForUserAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            if (!IsManagedMembership(membership.Id) || expectedMembershipIds.Contains(membership.Id))
            {
                continue;
            }

            if (await administrationService.DeleteMembershipAsync(membership.Id, cancellationToken).ConfigureAwait(false))
            {
                deletedMembershipIds.Add(membership.Id);
            }
        }

        return deletedMembershipIds.OrderBy(static item => item.Value, StringComparer.Ordinal).ToArray();
    }

    private async Task<IReadOnlyCollection<AccessAssignmentId>> DeleteStaleAssignmentsAsync(
        AccessUserId userId,
        IReadOnlySet<AccessAssignmentId> expectedAssignmentIds,
        CancellationToken cancellationToken)
    {
        if (!options.ReconcileMissingAssignments)
        {
            return Array.Empty<AccessAssignmentId>();
        }

        List<AccessAssignmentId> deletedAssignmentIds = [];
        await foreach (var assignment in queryService.GetAssignmentsForUserAsync(userId, cancellationToken).ConfigureAwait(false))
        {
            if (!IsManagedAssignment(assignment.Id) || expectedAssignmentIds.Contains(assignment.Id))
            {
                continue;
            }

            if (await administrationService.DeleteAssignmentAsync(assignment.Id, cancellationToken).ConfigureAwait(false))
            {
                deletedAssignmentIds.Add(assignment.Id);
            }
        }

        return deletedAssignmentIds.OrderBy(static item => item.Value, StringComparer.Ordinal).ToArray();
    }

    private IReadOnlyCollection<ExternalLink> MergeExternalLinks(
        IReadOnlyCollection<ExternalLink>? existingLinks,
        string organizationId)
    {
        List<ExternalLink> links = existingLinks?.ToList() ?? [];
        if (links.Any(IsOrganizationLink))
        {
            return links;
        }

        links.Add(new ExternalLink(
            new ExternalLinkId(MakeStableId(ScopeRootLinkIdPrefix, organizationId)),
            options.ProviderName,
            organizationId,
            options.OrganizationResourceType));

        return links;
    }

    private bool IsOrganizationLink(ExternalLink link) =>
        string.Equals(link.Provider, options.ProviderName, StringComparison.Ordinal)
        && string.Equals(link.ResourceType, options.OrganizationResourceType, StringComparison.Ordinal);

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(options.ProviderName))
        {
            throw new InvalidOperationException("WorkOS access options must define a provider name.");
        }

        if (string.IsNullOrWhiteSpace(options.OrganizationResourceType))
        {
            throw new InvalidOperationException("WorkOS access options must define an organization resource type.");
        }

        if (string.IsNullOrWhiteSpace(options.RoleAliasKey))
        {
            throw new InvalidOperationException("WorkOS access options must define a role alias key.");
        }
    }

    private static bool IsManagedMembership(ScopeMembershipId membershipId) =>
        membershipId.Value.StartsWith(MembershipIdPrefix + ":", StringComparison.Ordinal);

    private static bool IsManagedAssignment(AccessAssignmentId assignmentId) =>
        assignmentId.Value.StartsWith(AssignmentIdPrefix + ":", StringComparison.Ordinal);

    private static string MakeStableId(params string[] parts) =>
        string.Join(":", parts.Select(static item => Uri.EscapeDataString(item.Trim())));

    private static string MakeUniqueId(string prefix, string subject, DateTimeOffset timestamp) =>
        prefix
        + ":"
        + Uri.EscapeDataString(subject.Trim())
        + ":"
        + timestamp.UtcDateTime.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
}
