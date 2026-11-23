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

namespace Incursa.Platform.Tests;

public class SchemaActivationDefaultsTests
{
    /// <summary>When application database configurations are created, then implicit schema deployment is disabled.</summary>
    /// <intent>Keep DDL out of runtime identities unless an application explicitly opts into a controlled migration path.</intent>
    /// <scenario>Given default outbox, inbox, fanout, and lease database configurations.</scenario>
    /// <behavior>Then every configuration leaves EnableSchemaDeployment false.</behavior>
    [Fact]
    public void ApplicationDatabaseConfigurations_DisableSchemaDeploymentByDefault()
    {
        var outbox = new OutboxDatabaseConfig { Identifier = "outbox", ConnectionString = "configured-by-application" };
        var inbox = new InboxDatabaseConfig { Identifier = "inbox", ConnectionString = "configured-by-application" };
        var fanout = new FanoutDatabaseConfig { Identifier = "fanout", ConnectionString = "configured-by-application" };
        var lease = new LeaseDatabaseConfig { Identifier = "lease", ConnectionString = "configured-by-application" };

        outbox.EnableSchemaDeployment.ShouldBeFalse();
        inbox.EnableSchemaDeployment.ShouldBeFalse();
        fanout.EnableSchemaDeployment.ShouldBeFalse();
        lease.EnableSchemaDeployment.ShouldBeFalse();
    }
}
