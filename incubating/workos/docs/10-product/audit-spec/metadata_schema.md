# Metadata Schema

## Metadata Schema

Audit Log Events can contain arbitrary metadata for adding additional details to your events. Normally this data can take any shape. However, custom metadata schemas can be defined when configuring the event for additional type safety and data consistency. When an event is emitted that does not match the provided schema, an error will be returned.

When first creating an event schema, check the “Require metadata schema validation” checkbox. You will then be navigated to the schema editor where you can modify the underlying [JSON Schema](https://json-schema.org/) for all `metadata` objects.

![A screenshot showing how to require metadata schema validation in the WorkOS Dashboard.](https://images.workoscdn.com/images/24a410e1-72aa-4f5b-8854-98a4307602ff.png?auto=format\&fit=clip\&q=50)

There are `metadata` objects located at the root of the event, and within `actor` and `targets` objects. Each can contain a unique JSON Schema. To add to a `metadata` object, click the "+" sign.

> Metadata objects have a limit of 50 keys. Key names can be up to 40 characters long, and values can be up to 500 characters long.

![A screenshot showing the schema editor in the WorkOS Dashboard.](https://images.workoscdn.com/images/7d9e37a3-2e8d-4910-b85d-34c224e375be.png?auto=format\&fit=clip\&q=50)

#### Event with metadata

```js
import { WorkOS } from '@workos-inc/node';

const workos = new WorkOS('WORKOS_API_KEY_PLACEHOLDER');

await workos.auditLogs.createEvent('org_01EHWNCE74X7JSDV0X3SZ3KJNY', {
  action: 'user.signed_in',
  occurredAt: new Date(),
  actor: {
    type: 'user',
    id: 'user_01GBNJC3MX9ZZJW1FSTF4C5938',
    metadata: {
      role: 'admin',
    },
  },
  targets: [
    {
      type: 'team',
      id: 'team_01GBNJD4MKHVKJGEWK42JNMBGS',
      metadata: {
        owner: 'user_01GBTCQ2MZG9C87R7NAQZZS7M6',
      },
    },
  ],
  context: {
    location: '123.123.123.123',
    userAgent: 'Chrome/104.0.0.0',
  },
  metadata: {
    environment: 'staging',
  },
});
```

```rb
require "workos"

WorkOS.configure do |config|
  config.key = "WORKOS_API_KEY_PLACEHOLDER"
end

event = {
  action: "user.signed_in",
  occurred_at: "2022-09-08T19:46:03.435Z",
  actor: {
    id: "user_01GBNJC3MX9ZZJW1FSTF4C5938",
    type: "user",
    metadata: {
      role: "admin"
    }
  },
  targets: [
    {
      id: "team_01GBNJD4MKHVKJGEWK42JNMBGS",
      type: "team",
      metadata: {
        owner: "user_01GBTCQ2MZG9C87R7NAQZZS7M6"
      }
    }
  ],
  context: {
    location: "1.1.1.1",
    user_agent: "Chrome/104.0.0.0"
  },
  metadata: {
    environment: "staging"
  }
}

WorkOS::AuditLogs.create_event(
  organization: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
  event: event
)
```

```py
import datetime

from workos import WorkOSClient
from workos.types.audit_logs import AuditLogEvent

workos_client = WorkOSClient(
    api_key="WORKOS_API_KEY_PLACEHOLDER", client_id="WORKOS_CLIENT_ID_PLACEHOLDER"
)

organization_id = "org_01EHWNCE74X7JSDV0X3SZ3KJNY"
event: AuditLogEvent = {
    "action": "user.signed_in",
    "occurred_at": datetime.datetime.now(tz=datetime.UTC).isoformat(),
    "actor": {
        "type": "user",
        "id": "user_01GBNJC3MX9ZZJW1FSTF4C5938",
        "metadata": {
            "role": "admin",
        },
    },
    "targets": [
        {
            "type": "team",
            "id": "team_01GBNJD4MKHVKJGEWK42JNMBGS",
            "metadata": {
                "owner": "user_01GBTCQ2MZG9C87R7NAQZZS7M6",
            },
        },
    ],
    "context": {
        "location": "123.123.123.123",
        "user_agent": "Chrome/104.0.0.0",
    },
    "metadata": {
        "environment": "staging",
    },
}

workos_client.audit_logs.create_event(organization_id=organization_id, event=event)
```

```go
package main

import (
	"context"
	"time"

	"github.com/workos/workos-go/v3/pkg/auditlogs"
)

func main() {
	auditlogs.SetAPIKey("WORKOS_API_KEY_PLACEHOLDER")

	err := auditlogs.CreateEvent(context.Background(), auditlogs.CreateEventOpts{
		OrganizationID: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
		Event: auditlogs.Event{
			Action:     "user.signed_in",
			OccurredAt: time.Now(),
			Actor: auditlogs.Actor{
				ID:   "user_01GBNJC3MX9ZZJW1FSTF4C5938",
				Type: "user",
				Metadata: map[string]interface{}{
					"role": "admin",
				},
			},
			Targets: []auditlogs.Target{
				{
					ID:   "team_01GBNJD4MKHVKJGEWK42JNMBGS",
					Type: "team",
					Metadata: map[string]interface{}{
						"owner": "user_01GBTCQ2MZG9C87R7NAQZZS7M6",
					},
				},
			},
			Context: auditlogs.Context{
				Location:  "123.123.123.123",
				UserAgent: "Chrome/104.0.0.0",
			},
			Metadata: map[string]interface{}{
				"environment": "staging",
			},
		},
	})
}
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogEvent = [
    "action" => "user.signed_in",
    "occurred_at" => date("c"),
    "actor" => [
        "id" => "user_01GBNJC3MX9ZZJW1FSTF4C5938",
        "type" => "user",
        "metadata" => [
            "role" => "admin",
        ],
    ],
    "targets" => [
        [
            "id" => "team_01GBNJD4MKHVKJGEWK42JNMBGS",
            "type" => "team",
            "metadata" => [
                "owner" => "user_01GBTCQ2MZG9C87R7NAQZZS7M6",
            ],
        ],
    ],
    "context" => [
        "location" => "123.123.123.123",
        "user_agent" => "Chrome/104.0.0.0",
    ],
    "metadata" => [
        "environment" => "staging",
    ],
];

$auditLogs->createEvent(
    organizationId: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
    event: $auditLogEvent
);
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogEvent = [
    "action" => "user.signed_in",
    "occurred_at" => date("c"),
    "actor" => [
        "id" => "user_01GBNJC3MX9ZZJW1FSTF4C5938",
        "type" => "user",
        "metadata" => [
            "role" => "admin",
        ],
    ],
    "targets" => [
        [
            "id" => "team_01GBNJD4MKHVKJGEWK42JNMBGS",
            "type" => "team",
            "metadata" => [
                "owner" => "user_01GBTCQ2MZG9C87R7NAQZZS7M6",
            ],
        ],
    ],
    "context" => [
        "location" => "123.123.123.123",
        "user_agent" => "Chrome/104.0.0.0",
    ],
    "metadata" => [
        "environment" => "staging",
    ],
];

$auditLogs->createEvent(
    organizationId: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
    event: $auditLogEvent
);
```

```java
import com.workos.WorkOS;
import com.workos.auditlogs.AuditLogsApi.CreateAuditLogEventOptions;
import java.util.Date;
import java.util.Map;

WorkOS workos = new WorkOS("WORKOS_API_KEY_PLACEHOLDER");

CreateAuditLogEventOptions options =
    CreateAuditLogEventOptions.builder()
        .action("user.signed_in")
        .occurredAt(new Date())
        .actor("user_01GBNJC3MX9ZZJW1FSTF4C5938", "user", null, Map.of("role", "admin"))
        .target("team_01GBNJD4MKHVKJGEWK42JNMBGS",
            "team",
            null,
            Map.of("owner", "user_01GBTCQ2MZG9C87R7NAQZZS7M6"))
        .context("123.123.123.123", "Chrome/104.0.0.0")
        .metadata(Map.of("environment", "staging"))
        .build();

workos.auditLogs.createEvent("org_01EHWNCE74X7JSDV0X3SZ3KJNY", options);
```

```cs
WorkOS.SetApiKey("WORKOS_API_KEY_PLACEHOLDER");

var auditLogsService = new AuditLogsService();

var auditLogEvent = new AuditLogEvent {
    Action = "user.signed_in",
    OccurredAt = DateTime.Now,
    Actor =
        new AuditLogEventActor {
            Id = "user_01GBNJC3MX9ZZJW1FSTF4C5938",
            Type = "user",
            Metadata =
                new Dictionary<string, string> {
                    { "role", "admin" },
                },
        },
    Targets =
        new List<AuditLogEventTarget>() {
            new AuditLogEventTarget {
                Id = "team_123",
                Type = "team",
                Metadata =
                    new Dictionary<string, string> {
                        { "owner", "user_01GBTCQ2MZG9C87R7NAQZZS7M6" },
                    },
            },
        },
    Context =
        new AuditLogEventContext {
            Location = "123.123.123.123",
            UserAgent = "Chrome/104.0.0.0",
        },
    Metadata =
        new Dictionary<string, string> {
            { "environment", "staging" },
        },
};

var options =
    new CreateAuditLogEventOptions() { OrganizationId = "org_01EHWNCE74X7JSDV0X3SZ3KJNY", Event = auditLogEvent };

auditLogsService.CreateEvent(options);
```
