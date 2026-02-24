// Copyright (c) Incursa
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Net;
using Incursa.Platform.Health;
using Incursa.Platform.Health.AspNetCore;
using Incursa.Platform.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Incursa.Platform.Tests;

public sealed class StartupLatchLiveHealthEndpointTests
{
    /// <summary>When latches are held, live remains healthy and ready is unavailable until release.</summary>
    /// <intent>Verify standardized bucket behavior for startup latch.</intent>
    /// <scenario>Given a held startup latch and mapped standardized health endpoints.</scenario>
    /// <behavior>Then /healthz is 200 while /readyz is 503, and /readyz becomes 200 after release.</behavior>
    [Fact]
    public async Task StartupLatch_AffectsReadyOnly()
    {
        await using var app = await BuildAppAsync();
        var client = app.GetTestClient();

        var latch = app.Services.GetRequiredService<IStartupLatch>();
        using var step = latch.Register("platform-migrations");

        var liveWhileStarting = await client.GetAsync(new Uri(PlatformHealthEndpoints.Live, UriKind.Relative), TestContext.Current.CancellationToken);
        liveWhileStarting.StatusCode.ShouldBe(HttpStatusCode.OK);

        var readyWhileStarting = await client.GetAsync(new Uri(PlatformHealthEndpoints.Ready, UriKind.Relative), TestContext.Current.CancellationToken);
        readyWhileStarting.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        step.Dispose();

        var readyAfterRelease = await client.GetAsync(new Uri(PlatformHealthEndpoints.Ready, UriKind.Relative), TestContext.Current.CancellationToken);
        readyAfterRelease.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<WebApplication> BuildAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddPlatformObservability()
            .AddPlatformHealthChecks();

        var app = builder.Build();
        app.MapPlatformHealthEndpoints();

        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return app;
    }
}
