# Editing Events

## Editing Events

Once you’ve successfully configured Audit Logs in the WorkOS Dashboard and begun emitting events, how do you go about modifying an event schema without breaking your existing integrations? This is where versioning comes into place. When you make a modification to an existing schema it will create a new version rather than overwriting the existing schema.

The reason for this behavior is to ensure backwards compatibility. Schema configuration is immutable to prevent you from accidentally making changes that are incompatible with events that are already being emitted from your application. Rather you must first create a new version of the schema, and then explicitly emit events for that version leveraging the event `version` field.

### Creating a new event version

In the WorkOS Dashboard navigate to the Audit Logs configuration page. Locate the event that you would like to modify the schema for and click the “Edit Event” item under the context menu.

![A screenshot showing the "Edit Event" option in the WorkOS Dashboard.](https://images.workoscdn.com/images/8ee56828-fc59-4c18-ae64-008a754cd2a6.png?auto=format\&fit=clip\&q=50)

You will be navigated to a page where you can edit both the `targets` associated with the event, and optionally the metadata JSON schema. Once you’re done making changes, clicking save will create a new version of the event schema.

![A screenshot showing the "Save as new version" button in the schema editor in the WorkOS Dashboard.](https://images.workoscdn.com/images/65d234a3-f530-4051-95a0-0162cfef122e.png?auto=format\&fit=clip\&q=50)

### Emitting event with version

Now that a schema exists with a new version, the `version` field must be provided when emitting an event so that WorkOS knows which version to use for validation.

#### Emit event

```js
import { WorkOS } from '@workos-inc/node';

const workos = new WorkOS('WORKOS_API_KEY_PLACEHOLDER');

await workos.auditLogs.createEvent('org_01EHWNCE74X7JSDV0X3SZ3KJNY', {
  action: 'user.signed_in',
  version: 2,
  occurredAt: new Date(),
  actor: {
    type: 'user',
    id: 'user_01GBNJC3MX9ZZJW1FSTF4C5938',
  },
  targets: [
    {
      type: 'team',
      id: 'team_01GBNJD4MKHVKJGEWK42JNMBGS',
    },
    {
      type: 'resource',
      id: 'resource_01GBTAX2J37BAMB2D8GYDR2CC6',
    },
  ],
  context: {
    location: '123.123.123.123',
    userAgent: 'Chrome/104.0.0.0',
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
  version: 2,
  actor: {
    id: "user_01GBNJC3MX9ZZJW1FSTF4C5938",
    type: "user"
  },
  targets: [
    {
      id: "team_01GBNJD4MKHVKJGEWK42JNMBGS",
      type: "team"
    },
    {
      id: "resource_01GBTAX2J37BAMB2D8GYDR2CC6",
      type: "resource"
    }
  ],
  context: {
    location: "123.123.123.123",
    user_agent: "Chrome/104.0.0.0"
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
    "version": 2,  # version provided
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
			Version:    2,
			OccurredAt: time.Now(),
			Actor: auditlogs.Actor{
				ID:   "user_01GBNJC3MX9ZZJW1FSTF4C5938",
				Type: "user",
			},
			Targets: []auditlogs.Target{
				{
					ID:   "team_01GBNJD4MKHVKJGEWK42JNMBGS",
					Type: "team",
				},
			},
			Context: auditlogs.Context{
				Location:  "123.123.123.123",
				UserAgent: "Chrome/104.0.0.0",
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
    "version" => 2,
    "actor" => [
        "id" => "user_01GBNJC3MX9ZZJW1FSTF4C5938",
        "type" => "user",
    ],
    "targets" => [
        [
            "id" => "team_01GBNJD4MKHVKJGEWK42JNMBGS",
            "type" => "team",
        ],
        [
            "id" => "resource_01GBTAX2J37BAMB2D8GYDR2CC6",
            "type" => "resource",
        ],
    ],
    "context" => [
        "location" => "123.123.123.123",
        "user_agent" => "Chrome/104.0.0.0",
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
    "version" => 2,
    "actor" => [
        "id" => "user_01GBNJC3MX9ZZJW1FSTF4C5938",
        "type" => "user",
    ],
    "targets" => [
        [
            "id" => "team_01GBNJD4MKHVKJGEWK42JNMBGS",
            "type" => "team",
        ],
        [
            "id" => "resource_01GBTAX2J37BAMB2D8GYDR2CC6",
            "type" => "resource",
        ],
    ],
    "context" => [
        "location" => "123.123.123.123",
        "user_agent" => "Chrome/104.0.0.0",
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

WorkOS workos = new WorkOS("WORKOS_API_KEY_PLACEHOLDER");

CreateAuditLogEventOptions options =
    CreateAuditLogEventOptions.builder()
        .action("user.signed_in")
        .version(2)
        .occurredAt(new Date())
        .actor("user_01GBNJC3MX9ZZJW1FSTF4C5938", "user")
        .target("team_01GBNJD4MKHVKJGEWK42JNMBGS", "team")
        .target("resource_01GBTAX2J37BAMB2D8GYDR2CC6", "resource")
        .context("123.123.123.123", "Chrome/104.0.0.0")
        .build();

workos.auditLogs.createEvent("org_01EHWNCE74X7JSDV0X3SZ3KJNY", options);
```

```cs
WorkOS.SetApiKey("WORKOS_API_KEY_PLACEHOLDER");

var auditLogsService = new AuditLogsService();

var auditLogEvent = new AuditLogEvent {
    Action = "user.signed_in",
    OccurredAt = DateTime.Now,
    Version = 2,
    Actor =
        new AuditLogEventActor {
            Id = "user_01GBNJC3MX9ZZJW1FSTF4C5938",
            Type = "user",
        },
    Targets =
        new List<AuditLogEventTarget>() {
            new AuditLogEventTarget {
                Id = "team_01GBNJD4MKHVKJGEWK42JNMBGS",
                Type = "team",
            },
            new AuditLogEventTarget {
                Id = "resource_01GBTAX2J37BAMB2D8GYDR2CC6",
                Type = "resource",
            },
        },
    Context =
        new AuditLogEventContext {
            Location = "123.123.123.123",
            UserAgent = "Chrome/104.0.0.0",
        },
};

var options =
    new CreateAuditLogEventOptions() { OrganizationId = "org_01EHWNCE74X7JSDV0X3SZ3KJNY", Event = auditLogEvent };

auditLogsService.CreateEvent(options);
```
