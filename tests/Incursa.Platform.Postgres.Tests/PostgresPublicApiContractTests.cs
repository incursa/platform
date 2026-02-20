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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Tests;

[Trait("Category", "Unit")]
public sealed class PostgresPublicApiContractTests
{
    /// <summary>When AddPostgresPlatform is used, then core routing dependencies are registered.</summary>
    /// <intent>Verify primary Postgres platform registration wires primitive routers/providers and platform services.</intent>
    /// <scenario>Given a service collection configured with AddPostgresPlatform.</scenario>
    /// <behavior>Outbox/inbox/scheduler store providers and discovery services are registered.</behavior>
    [Fact]
    public void AddPostgresPlatform_RegistersCoreDependencies()
    {
        var services = new ServiceCollection();
        services.AddPostgresPlatform("Host=localhost;Database=test;Username=test;Password=test");

        services.Any(d => d.ServiceType == typeof(IOutboxStoreProvider)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(IInboxWorkStoreProvider)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(ISchedulerStoreProvider)).ShouldBeTrue();
        services.Any(d => d.ServiceType == typeof(IPlatformDatabaseDiscovery)).ShouldBeTrue();
    }

    /// <summary>When list-based registration is repeated, then registration guard rails reject duplicate setup.</summary>
    /// <intent>Ensure platform registration lifecycle validates duplicate/invalid registration state.</intent>
    /// <scenario>Given AddPostgresPlatformMultiDatabaseWithList called twice on the same service collection.</scenario>
    /// <behavior>The second call throws InvalidOperationException.</behavior>
    [Fact]
    public void AddPostgresPlatformMultiDatabaseWithList_CalledTwice_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddPostgresPlatformMultiDatabaseWithList(new[]
        {
            new PlatformDatabase { Name = "db1", ConnectionString = "Host=localhost;Database=db1;Username=test;Password=test" },
        }, enableSchemaDeployment: false);

        Assert.Throws<InvalidOperationException>(() =>
            services.AddPostgresPlatformMultiDatabaseWithList(new[]
            {
                new PlatformDatabase { Name = "db2", ConnectionString = "Host=localhost;Database=db2;Username=test;Password=test" },
            }, enableSchemaDeployment: false));
    }

    /// <summary>When invalid outbox options are supplied, then options validation fails immediately.</summary>
    /// <intent>Ensure Postgres option validators enforce required contracts.</intent>
    /// <scenario>Given AddPostgresOutbox called with an empty connection string.</scenario>
    /// <behavior>An OptionsValidationException is thrown.</behavior>
    [Fact]
    public void AddPostgresOutbox_ThrowsForMissingConnectionString()
    {
        var services = new ServiceCollection();

        Assert.Throws<OptionsValidationException>(() =>
            services.AddPostgresOutbox(new PostgresOutboxOptions
            {
                ConnectionString = string.Empty,
            }));
    }
}
