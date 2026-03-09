namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.AspNetCore.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

[TestClass]
public sealed class WorkOsUserProfileRefreshMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_AuthenticatedPrincipal_EnrichesOncePerRequest()
    {
        var enricher = new RecordingClaimsEnricher();
        var middleware = new WorkOsUserProfileRefreshMiddleware(
            enricher,
            new WorkOsUserProfileHydrationOptions
            {
                Enabled = true,
                RevalidateOnRequestIfStale = true,
            },
            NullLogger<WorkOsUserProfileRefreshMiddleware>.Instance);

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_123")], "test")),
        };

        await middleware.InvokeAsync(context, static _ => Task.CompletedTask).ConfigureAwait(false);
        await middleware.InvokeAsync(context, static _ => Task.CompletedTask).ConfigureAwait(false);

        Assert.AreEqual(1, enricher.CallCount);
    }

    private sealed class RecordingClaimsEnricher : IWorkOsClaimsEnricher
    {
        public int CallCount { get; private set; }

        public ValueTask EnrichAsync(ClaimsPrincipal principal, ClaimsIdentity identity, CancellationToken ct = default)
        {
            CallCount++;
            return ValueTask.CompletedTask;
        }
    }
}
