namespace Incursa.Platform.Access.AspNetCore.Tests;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

[Trait("Category", "Unit")]
public sealed class CurrentAccessContextAccessorTests
{
    [Fact]
    public async Task GetCurrentAsync_MapsWorkOsClaimsIntoCurrentOrganizationScopeAsync()
    {
        var query = Substitute.For<IAccessQueryService>();
        var user = new AccessUser(new AccessUserId("user-1"), "Ada Lovelace");
        var scopeRootOne = new ScopeRoot(new ScopeRootId("scope-org-1"), ScopeRootKind.Organization, "Northwind");
        var scopeRootTwo = new ScopeRoot(new ScopeRootId("scope-org-2"), ScopeRootKind.Organization, "Contoso");

        query.GetUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        query.GetMembershipsForUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(
        [
            new ScopeMembership(new ScopeMembershipId("membership-1"), user.Id, scopeRootOne.Id, DateTimeOffset.UtcNow),
            new ScopeMembership(new ScopeMembershipId("membership-2"), user.Id, scopeRootTwo.Id, DateTimeOffset.UtcNow),
        ]));
        query.GetScopeRootByExternalLinkAsync("workos", "org_1", "organization", Arg.Any<CancellationToken>()).Returns(scopeRootOne);
        query.GetScopeRootByExternalLinkAsync("workos", "org_2", "organization", Arg.Any<CancellationToken>()).Returns(scopeRootTwo);
        query.GetAccessibleTenantsAsync(user.Id, Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(Array.Empty<Tenant>()));

        var httpContext = CreateHttpContext(
            [
                new Claim("sub", "user-1"),
                new Claim("org_ids", "[\"org_1\",\"org_2\"]"),
            ]);
        httpContext.Request.RouteValues["organizationId"] = "org_2";

        await using var services = CreateServices(query, httpContext);
        var accessor = services.GetRequiredService<ICurrentAccessContextAccessor>();

        var context = await accessor.GetCurrentAsync(TestContext.Current.CancellationToken);

        context.UserId.ShouldBe(user.Id);
        context.User.ShouldBe(user);
        context.ScopeRoot.ShouldBe(scopeRootTwo);
        context.Tenant.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsAuthenticatedUserWithoutScopeWhenNoSelectionResolvesAsync()
    {
        var query = Substitute.For<IAccessQueryService>();
        var user = new AccessUser(new AccessUserId("user-2"), "Grace Hopper");

        query.GetUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        query.GetMembershipsForUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Array.Empty<ScopeMembership>()));
        query.GetPersonalScopeRootAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((ScopeRoot?)null);
        query.GetAccessibleTenantsAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Array.Empty<Tenant>()));

        var httpContext = CreateHttpContext([new Claim("sub", "user-2")]);

        await using var services = CreateServices(query, httpContext);
        var accessor = services.GetRequiredService<ICurrentAccessContextAccessor>();

        var context = await accessor.GetCurrentAsync(TestContext.Current.CancellationToken);

        context.IsAuthenticated.ShouldBeTrue();
        context.UserId.ShouldBe(user.Id);
        context.ScopeRoot.ShouldBeNull();
        context.Tenant.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_FallsBackToPersonalScopeWhenNoOrganizationScopeResolvesAsync()
    {
        var query = Substitute.For<IAccessQueryService>();
        var userId = new AccessUserId("user-3");
        var personalScope = new ScopeRoot(new ScopeRootId("personal-3"), ScopeRootKind.Personal, "Personal", userId);

        query.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AccessUser(userId, "Linus Torvalds"));
        query.GetMembershipsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Array.Empty<ScopeMembership>()));
        query.GetPersonalScopeRootAsync(userId, Arg.Any<CancellationToken>())
            .Returns(personalScope);
        query.GetAccessibleTenantsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Array.Empty<Tenant>()));

        var httpContext = CreateHttpContext([new Claim("sub", userId.Value)]);

        await using var services = CreateServices(query, httpContext);
        var accessor = services.GetRequiredService<ICurrentAccessContextAccessor>();

        var context = await accessor.GetCurrentAsync(TestContext.Current.CancellationToken);

        context.ScopeRoot.ShouldBe(personalScope);
        context.ScopeRoot!.Kind.ShouldBe(ScopeRootKind.Personal);
        context.ScopeRoot.OwnerUserId.ShouldBe(userId);
    }

    [Fact]
    public async Task GetCurrentAsync_ResolvesTenantWithinCurrentScopeAsync()
    {
        var query = Substitute.For<IAccessQueryService>();
        var userId = new AccessUserId("user-4");
        var scopeRoot = new ScopeRoot(new ScopeRootId("scope-org-4"), ScopeRootKind.Organization, "Fabrikam");
        var tenant = new Tenant(new TenantId("tenant-4"), scopeRoot.Id, "Tenant Four");

        query.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AccessUser(userId, "Barbara Liskov"));
        query.GetMembershipsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(
            [
                new ScopeMembership(new ScopeMembershipId("membership-4"), userId, scopeRoot.Id, DateTimeOffset.UtcNow),
            ]));
        query.GetScopeRootByExternalLinkAsync("workos", "org_4", "organization", Arg.Any<CancellationToken>())
            .Returns(scopeRoot);
        query.GetAccessibleTenantsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([tenant]));

        var httpContext = CreateHttpContext(
            [
                new Claim("sub", userId.Value),
                new Claim("org_id", "org_4"),
            ]);
        httpContext.Request.RouteValues["tenantId"] = tenant.Id.Value;

        await using var services = CreateServices(query, httpContext);
        var accessor = services.GetRequiredService<ICurrentAccessContextAccessor>();

        var context = await accessor.GetCurrentAsync(TestContext.Current.CancellationToken);

        context.ScopeRoot.ShouldBe(scopeRoot);
        context.Tenant.ShouldBe(tenant);
    }

    [Fact]
    public async Task GetCurrentAsync_IgnoresTenantOutsideResolvedScopeAsync()
    {
        var query = Substitute.For<IAccessQueryService>();
        var userId = new AccessUserId("user-5");
        var scopeRoot = new ScopeRoot(new ScopeRootId("scope-org-5"), ScopeRootKind.Organization, "Tailspin");
        var otherScopeRoot = new ScopeRoot(new ScopeRootId("scope-org-6"), ScopeRootKind.Organization, "Wingtip");
        var tenant = new Tenant(new TenantId("tenant-5"), otherScopeRoot.Id, "Other Tenant");

        query.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AccessUser(userId, "Margaret Hamilton"));
        query.GetMembershipsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(
            [
                new ScopeMembership(new ScopeMembershipId("membership-5"), userId, scopeRoot.Id, DateTimeOffset.UtcNow),
                new ScopeMembership(new ScopeMembershipId("membership-6"), userId, otherScopeRoot.Id, DateTimeOffset.UtcNow),
            ]));
        query.GetScopeRootByExternalLinkAsync("workos", "org_5", "organization", Arg.Any<CancellationToken>())
            .Returns(scopeRoot);
        query.GetAccessibleTenantsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([tenant]));

        var httpContext = CreateHttpContext(
            [
                new Claim("sub", userId.Value),
                new Claim("org_id", "org_5"),
            ]);
        httpContext.Request.RouteValues["tenantId"] = tenant.Id.Value;

        await using var services = CreateServices(query, httpContext);
        var accessor = services.GetRequiredService<ICurrentAccessContextAccessor>();

        var context = await accessor.GetCurrentAsync(TestContext.Current.CancellationToken);

        context.ScopeRoot.ShouldBe(scopeRoot);
        context.Tenant.ShouldBeNull();
    }

    [Fact]
    public async Task AddAccessAspNetCore_RegistersAccessorAndConfiguredOptionsAsync()
    {
        var query = Substitute.For<IAccessQueryService>();
        var userId = new AccessUserId("user-6");
        var scopeRoot = new ScopeRoot(new ScopeRootId("scope-org-6"), ScopeRootKind.Organization, "Configured");

        query.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AccessUser(userId, "Donald Knuth"));
        query.GetMembershipsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(
            [
                new ScopeMembership(new ScopeMembershipId("membership-7"), userId, scopeRoot.Id, DateTimeOffset.UtcNow),
            ]));
        query.GetScopeRootByExternalLinkAsync("custom-provider", "org_custom", "custom-org", Arg.Any<CancellationToken>())
            .Returns(scopeRoot);
        query.GetAccessibleTenantsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Array.Empty<Tenant>()));

        var httpContext = CreateHttpContext(
            [
                new Claim("sub", userId.Value),
                new Claim("organization", "org_custom"),
            ]);

        var services = new ServiceCollection();
        services.AddSingleton(query);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
        services.AddAccessAspNetCore(options =>
        {
            options.ScopeRootExternalLinkProvider = "custom-provider";
            options.ScopeRootExternalLinkResourceType = "custom-org";
        });

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentAccessContextAccessor>();

        var context = await accessor.GetCurrentAsync(TestContext.Current.CancellationToken);

        context.ScopeRoot.ShouldBe(scopeRoot);
        await query.Received(1).GetScopeRootByExternalLinkAsync(
            "custom-provider",
            "org_custom",
            "custom-org",
            Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateServices(IAccessQueryService queryService, HttpContext httpContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton(queryService);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
        services.AddAccessAspNetCore();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(IEnumerable<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
