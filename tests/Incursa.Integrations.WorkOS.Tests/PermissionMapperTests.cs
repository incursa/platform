namespace Incursa.Integrations.WorkOS.Tests;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Core.Authorization;

[TestClass]
public sealed class PermissionMapperTests
{
    [TestMethod]
    public void MapToScopes_NormalizesAndDeduplicates()
    {
        var options = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        var mapper = new WorkOsPermissionMapper(options);

        var scopes = mapper.MapToScopes(["NuGet.Read", "nuget.read", "raw.push"], strictMode: true, out var unknown);

        Assert.IsEmpty(unknown);
        CollectionAssert.AreEquivalent(new[] { "nuget.read", "raw.push" }, scopes.ToArray());
    }

    [TestMethod]
    public void MapToScopes_StrictModeWithUnknown_ReturnsEmpty()
    {
        var options = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        var mapper = new WorkOsPermissionMapper(options);

        var scopes = mapper.MapToScopes(["nuget.read", "unknown.permission"], strictMode: true, out var unknown);

        Assert.HasCount(1, unknown);
        Assert.IsEmpty(scopes);
    }
}

