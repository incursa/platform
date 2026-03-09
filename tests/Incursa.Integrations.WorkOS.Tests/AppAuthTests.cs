namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Claims;
using Incursa.Integrations.WorkOS;
using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class AppAuthTests
{
    [TestMethod]
    public void AddWorkOsAppAuth_RegistersClaimsAccessorAndPolicies()
    {
        ServiceCollection services = new();
        services.AddWorkOsAppAuth();
        var sp = services.BuildServiceProvider();

        var claimsAccessor = sp.GetService<IWorkOsClaimsAccessor>();
        var policyProvider = sp.GetService<IAuthorizationPolicyProvider>();

        Assert.IsNotNull(claimsAccessor);
        Assert.IsNotNull(policyProvider);
    }

    [TestMethod]
    public void ClaimsAccessor_NormalizesClaimSetsAcrossCandidateTypes()
    {
        ServiceCollection services = new();
        services.AddWorkOsAppAuth();
        var sp = services.BuildServiceProvider();
        var claimsAccessor = sp.GetRequiredService<IWorkOsClaimsAccessor>();

        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim("sub", "user-123"),
            new Claim("org_ids", "[\"org-a\",\"org-b\"]"),
            new Claim("workos:role", "owner"),
            new Claim("permissions", "one two"),
            new Claim("workos:permission", "three"),
        ], "test"));

        var claimSet = claimsAccessor.Read(principal);

        Assert.AreEqual("user-123", claimSet.Subject);
        CollectionAssert.AreEquivalent(new[] { "org-a", "org-b" }, claimSet.OrganizationIds.ToArray());
        CollectionAssert.AreEquivalent(new[] { "owner" }, claimSet.Roles.ToArray());
        CollectionAssert.AreEquivalent(new[] { "one", "two", "three" }, claimSet.Permissions.ToArray());
    }
}
