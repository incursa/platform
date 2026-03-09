# Audit Logs

## Introduction

Audit Logs are a collection of events that contain information relevant to notable actions taken by users in your application. Every event in the collection contains details regarding what kind of action was taken (`action`), who performed the action (`actor`), what resources were affected by the action (`targets`), and additional details of when and where the action took place.

```json
{
  "action": "user.signed_in",
  "occurred_at": "2022-08-29T19:47:52.336Z",
  "actor": {
    "type": "user",
    "id": "user_01GBNJC3MX9ZZJW1FSTF4C5938"
  },
  "targets": [
    {
      "type": "team",
      "id": "team_01GBNJD4MKHVKJGEWK42JNMBGS"
    }
  ],
  "context": {
    "location": "123.123.123.123",
    "user_agent": "Chrome/104.0.0.0"
  }
}
```

These events are similar to application logs and analytic events, but are fundamentally different in their intent. They aren’t typically used for active monitoring/alerting, rather they exist as a paper trail of potentially sensitive actions taken by members of an organization for compliance and security reasons.

## What you’ll build

This guide will show you how to:

1. Configure and emit Audit Log Events
2. Export Audit Log Events
3. Create custom metadata schemas for Audit Log Events
4. Create new versions of Audit Log Event schemas

## Before getting started

To get the most out of this guide, you’ll need:

- A [WorkOS account](https://dashboard.workos.com/)

## API object definitions

[Audit Log Event](https://workos.com/docs/reference/audit-logs/event/create)
: An individual event that represents an action taken by an actor within your app.

[Audit Log Export](https://workos.com/docs/reference/audit-logs/export)
: A collection of Audit Log Events that are exported from WorkOS as a CSV file.

[Organization](https://workos.com/docs/reference/organization)
: Describes a customer where Audit Log Events originate from.

## Emit an Audit Log Event

### Install the WorkOS SDK

WorkOS offers native SDKs in several popular programming languages. Choose a language below to see instructions in your application’s language.

### Set secrets

To make calls to WorkOS, provide the API key and, in some cases, the client ID. Store these values as managed secrets, such as `WORKOS_API_KEY` and `WORKOS_CLIENT_ID`, and pass them to the SDKs either as environment variables or directly in your app's configuration based on your preferences.

```plain title="Environment variables"
WORKOS_API_KEY='WORKOS_API_KEY_PLACEHOLDER'
WORKOS_CLIENT_ID='WORKOS_CLIENT_ID_PLACEHOLDER'
```

### Sign in to your WorkOS Dashboard account and configure Audit Log Event schemas

Before you can emit any Audit Log Events you must configure the allowed event schemas. To start, click “Create an event” and enter `user.signed_in` for action, `team` for targets, and click “Save event”.

![A screenshot showing how to create an audit log event in the WorkOS dashboard.](https://images.workoscdn.com/images/7658a3b2-1467-4c38-a98f-f99f933c5969.png?auto=format\&fit=clip\&q=50)

### Get an Organization ID

All events are scoped to an Organization, so you will need the ID of an Organization in order to emit events.

![A screenshot showing where to find an Organization ID in the WorkOS dashboard.](https://images.workoscdn.com/images/b76c7593-1d85-4f28-951e-24f177b8c233.png?auto=format\&fit=clip\&q=50)

### Emit Events

Using the ID from the Organization, emit an Audit Log Event with the `action` and `targets` previously configured.

#### Emit event

```js
import { WorkOS } from '@workos-inc/node';

const workos = new WorkOS('WORKOS_API_KEY_PLACEHOLDER');

await workos.auditLogs.createEvent('org_01EHWNCE74X7JSDV0X3SZ3KJNY', {
  action: 'user.signed_in',
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
  actor: {
    id: "user_01GBNJC3MX9ZZJW1FSTF4C5938",
    type: "user"
  },
  targets: [
    {
      id: "team_01GBNJD4MKHVKJGEWK42JNMBGS",
      type: "team"
    }
  ],
  context: {
    location: "1.1.1.1",
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
    "occurred_at": datetime.datetime.now(tz=datetime.UTC).isoformat(),
    "actor": {
        "type": "user",
        "id": "user_01GBNJC3MX9ZZJW1FSTF4C5938",
    },
    "targets": [
        {
            "type": "team",
            "id": "team_01GBNJD4MKHVKJGEWK42JNMBGS",
        },
    ],
    "context": {
        "location": "123.123.123.123",
        "user_agent": "Chrome/104.0.0.0",
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
			},
			Targets: []auditlogs.Target{
				{ID: "team_01GBNJD4MKHVKJGEWK42JNMBGS", Type: "team"},
			},
			Context: auditlogs.Context{
				Location:  "123.123.123.123",
				UserAgent: "Chrome/104.0.0.0",
			},
		},
	})
	if err != nil {
		// Handle the error...
	}
}
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogEvent = [
    "action" => "user.signed_in",
    "occurred_at" => date("c"),
    "version" => 1,
    "actor" => [
        "id" => "user_123",
        "type" => "user",
        "name" => "Jon Smith",
        "metadata" => [
            "role" => "admin",
        ],
    ],
    "targets" => [
        [
            "id" => "user_123",
            "type" => "user",
            "name" => "Jon Smith",
        ],
        [
            "id" => "user_123",
            "type" => "team",
            "metadata" => [
                "extra" => "data",
            ],
        ],
    ],
    "context" => [
        "location" => "1.1.1.1",
        "user_agent" => "Chrome/104.0.0.0",
    ],
    "metadata" => [
        "extra" => "data",
    ],
];

$idempotencyKey = "884793cd-bef4-46cf-8790-ed49257a09c6";

$auditLogs->createEvent(
    organizationId: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
    event: $auditLogEvent,
    idempotencyKey: $idempotencyKey
);
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogEvent = [
    "action" => "user.signed_in",
    "occurred_at" => date("c"),
    "version" => 1,
    "actor" => [
        "id" => "user_123",
        "type" => "user",
        "name" => "Jon Smith",
        "metadata" => [
            "role" => "admin",
        ],
    ],
    "targets" => [
        [
            "id" => "user_123",
            "type" => "user",
            "name" => "Jon Smith",
        ],
        [
            "id" => "user_123",
            "type" => "team",
            "metadata" => [
                "extra" => "data",
            ],
        ],
    ],
    "context" => [
        "location" => "1.1.1.1",
        "user_agent" => "Chrome/104.0.0.0",
    ],
    "metadata" => [
        "extra" => "data",
    ],
];

$idempotencyKey = "884793cd-bef4-46cf-8790-ed49257a09c6";

$auditLogs->createEvent(
    organizationId: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
    event: $auditLogEvent,
    idempotencyKey: $idempotencyKey
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
        .occurredAt(new Date())
        .actor("user_01GBNJC3MX9ZZJW1FSTF4C5938", "user")
        .target("team_01GBNJD4MKHVKJGEWK42JNMBGS", "team")
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

#### Idempotency

WorkOS Audit Logs supports idempotency to ensure events are not duplicated when retrying requests. You can provide an `idempotency-key` header with your event creation request. If you don't provide one, WorkOS will automatically generate one based on the event content.

When you provide an idempotency key:

- WorkOS creates a hashed key combining your provided key with the event data
- Subsequent requests with the same idempotency key and event data will return the same response
- This prevents duplicate events from being created due to network retries or other issues

When you don't provide an idempotency key:

- WorkOS automatically generates one using the event content
- This provides basic duplicate protection based on event data alone

### View ingested events in the Dashboard

Once you have successfully emitted events with the WorkOS SDK, you can view them in the Dashboard under the Organization that the events are associated with.

![A screenshot showing Audit Log events for an organization in the WorkOS dashboard.](https://images.workoscdn.com/images/b03dfaa4-c76a-4d08-a322-53458ba8b24d.png?auto=format\&fit=clip\&q=50)
