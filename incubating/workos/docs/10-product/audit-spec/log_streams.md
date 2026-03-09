# Log Streams

## Understanding Log Streams

Log Streams allow your customers to stream Audit Logs directly to their Security Incident and Event Management (SIEM) providers like Datadog or Splunk and object storage solutions like AWS S3 or Google Cloud Storage. There is also a generic provider (HTTP POST) available to stream logs to any configured endpoint.

This gives your customers greater control over their Audit Logs by allowing them to apply custom indexing and monitoring of their events in the SIEM provider along with events from other cloud services they use.

Log Streams can be created by either configuring the Log Stream through your WorkOS Dashboard or by allowing your customer's IT admin to configure it themselves through the WorkOS Admin Portal.

### IP allowlist

WorkOS streams audit logs from a fixed set of IP addresses. If audit logs are being streamed to a host that restricts access based on IP address, the following IP addresses should be allowed:

```plain title="WorkOS IP addresses"
3.217.146.166
23.21.184.92
34.204.154.149
44.213.245.178
44.215.236.82
50.16.203.9
52.1.251.34
52.21.49.187
174.129.36.47
```

## Dashboard

To configure a Log Stream through the WorkOS Dashboard, navigate to an organization and click “Configure”.

![A screenshot showing where to find "Configure" in the WorkOS Dashboard.](https://images.workoscdn.com/images/b555ad16-fce2-4014-997d-3d75b85f7860.png?auto=format\&fit=clip\&q=50)

You will be prompted to select a destination from a dropdown, click “Save connection”. You will then be prompted to provide specific configuration for the selected destination.

![A screenshot showing "Save connection" in the WorkOS Dashboard.](https://images.workoscdn.com/images/75ced694-5dbd-48c3-9784-5fdaf81e0420.png?auto=format\&fit=clip\&q=50)

## Admin Portal

The Admin Portal can be accessed via a Setup Link found in the Organization page within the Dashboard. Click “Generate” and select “Log Streams”. Copy the link and send it to the organization's IT admin who will be configuring Log Streams.

![A screenshot showing where the "Generate" button is located in the WorkOS Dashboard.](https://images.workoscdn.com/images/f6410460-40e4-478c-b663-5920ac15b8a8.png?auto=format\&fit=clip\&q=50)

You can also guide users to the Admin Portal by redirecting them to a programmatically generated Admin Portal link directly from your application.

#### Create Admin Portal Link for Log Streams

```js
import { WorkOS } from '@workos-inc/node';

const workos = new WorkOS('WORKOS_API_KEY_PLACEHOLDER');

const { link } = await workos.portal.generateLink({
  organization: 'org_01EHZNVPK3SFK441A1RGBFSHRT',
  intent: 'log_streams',
});

// Redirect to link
```

```rb
require "workos"

# The ID of the organization to start an Admin Portal session for
organization_id = "org_123"

link = WorkOS::Portal.generate_link(
  organization: organization_id,
  intent: "log_streams"
)

# Redirect to link
```

```py
from workos import WorkOSClient

workos_client = WorkOSClient(
    api_key="WORKOS_API_KEY_PLACEHOLDER", client_id="WORKOS_CLIENT_ID_PLACEHOLDER"
)

organization_id = (
    "org_123"  # The ID of the organization to start an Admin Portal session for
)

portal_link = workos_client.portal.generate_link(
    organization_id=organization_id, intent="log_streams"
)

# Redirect to portal_link["link"]
```

```go
package main

import (
	"context"
	"os"

	"github.com/workos/workos-go/v3/pkg/portal"
)

func main() {
	apiKey := os.Getenv("WORKOS_API_KEY")

	portal.SetAPIKey(apiKey)

	// The ID of the organization to start an Admin Portal session for
	organizationID := "org_123"

	link, err := portal.GenerateLink(context.Background(), portal.GenerateLinkOpts{
		Organization: organizationID,
		Intent:       portal.LogStreams,
	})

	if err != nil {
		// Handle the error...
	}

	// Redirect to link.Link
}
```

```php
<?php

$portal = new WorkOS\Portal();

$portalLink = $portal->generateLink(
    organization: "org_123",
    intent: "log_streams"
);

$url = portalLink["link"];

// Redirect to $url
```

```php
<?php

$portal = new WorkOS\Portal();

$portalLink = $portal->generateLink(
    organization: "org_123",
    intent: "log_streams"
);

$url = portalLink["link"];

// Redirect to $url
```

```java
import com.workos.WorkOS;
import com.workos.portal.PortalApi.GeneratePortalLinkOptions;
import com.workos.portal.models.Intent;
import com.workos.portal.models.Link;

Map<String, String> env = System.getenv();
WorkOS workos = new WorkOS(env.get("WORKOS_API_KEY"));

// The ID of the organization to start an Admin Portal session for
String organizationId = "org_123"

    Link response = workos.portal.generateLink(GeneratePortalLinkOptions.builder()
                                                   .organization()
                                                   .intent(Intent.LogStreams)
                                                   .build());

String link = response.link

              // Redirect to response.link
```

```cs
using System;
using WorkOS;

var portalService = new PortalService();

// The ID of the organization to start an Admin Portal session for
string organizationId = "org_123";

var options = new GenerateLinkOptions {
    Intent = Intent.LogStreams,
    Organization = organizationId,
};

var link = await portalService.GenerateLink(options);

// Redirect to the portal link
```

Once redirected to the Admin Portal, the user will be prompted to select a destination and will be provided with step-by-step configuration instructions for the selected destination.

![A screenshot showing log stream destination options in the WorkOS Admin Portal.](https://images.workoscdn.com/images/a6249873-d221-49eb-9c6a-c7706b2b4f77.png?auto=format\&fit=clip\&q=50)

## Streaming Destinations and Payload Formats

WorkOS supports streaming audit log events to six different types of destinations, each with its own payload format and configuration requirements:

### Datadog

Events are sent to Datadog's HTTP Log Intake API with regional endpoint support.

**Example Payload:**

```json
[
  {
    "message": {
      "id": "01HY123456ABCDEFGHIJK",
      "action": "user.signed_in",
      "targets": [
        {
          "id": "user_123",
          "type": "user"
        }
      ],
      "actor": {
        "id": "user_456",
        "type": "user"
      },
      "context": {
        "location": "192.168.1.1",
        "user_agent": "Chrome/91.0"
      },
      "occurred_at": "2024-01-15T10:30:00.000Z"
    },
    "ddsource": "team-name",
    "service": "audit-logs"
  }
]
```

**Configuration:**

- API Key authentication
- Regional endpoints (US1, US3, US5, EU1, US1-FED, AP1)
- Optional team name as source identifier

### Splunk

Events are sent to Splunk's HTTP Event Collector (HEC) endpoint.

**Example Payload:**

```json
[
  {
    "event": {
      "id": "01HY123456ABCDEFGHIJK",
      "action": "user.signed_in",
      "targets": [
        {
          "id": "user_123",
          "type": "user"
        }
      ],
      "actor": {
        "id": "user_456",
        "type": "user"
      },
      "context": {
        "location": "192.168.1.1",
        "user_agent": "Chrome/91.0"
      },
      "occurred_at": "2024-01-15T10:30:00.000Z"
    },
    "time": 1705314600000,
    "source": "team-name"
  }
]
```

**Configuration:**

- HEC Token authentication
- Custom Splunk instance URL
- Optional source identifier

### AWS S3

Events are stored as individual JSON files in an S3 bucket. We use a cross-account IAM role with an external ID
([details](https://docs.aws.amazon.com/IAM/latest/UserGuide/tutorial_cross-account-with-roles.html))
to authenticate to the destination bucket. We upload S3 objects with a `ContentMD5` header to support
[uploading objects to Object Lock enabled buckets](https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-lock-managing.html#object-lock-put-object).

| Property            | Description                                                    |
| ------------------- | -------------------------------------------------------------- |
| File Format         | Individual JSON files per event with pretty-printed formatting |
| File Naming Pattern | `YYYY-MM-DD/{timestamp}_{keySuffix}.json`                      |
| Example Filename    | `2024-01-15/2024-01-15T10:30:00.123Z_abc123def456.json`        |

**Example File Content:**

```json
{
  "id": "01HY123456ABCDEFGHIJK",
  "action": "user.signed_in",
  "targets": [
    {
      "id": "user_123",
      "type": "user"
    }
  ],
  "actor": {
    "id": "user_456",
    "type": "user"
  },
  "context": {
    "location": "192.168.1.1",
    "user_agent": "Chrome/91.0"
  },
  "occurred_at": "2024-01-15T10:30:00.000Z"
}
```

**Configuration:**

WorkOS authenticates to the destination S3 bucket using an AWS cross-account IAM role delegation with an external ID for enhanced security. This requires the following configuration:

| Field Name     | Code         | Description                                                              |
| -------------- | ------------ | ------------------------------------------------------------------------ |
| AWS Account ID | `accountId`  | Destination AWS account ID where the S3 bucket is located                |
| AWS Region     | `region`     | The AWS region for the destination S3 bucket (defaults to `us-east-1`)   |
| IAM Role Name  | `roleName`   | The name of the IAM role WorkOS will assume to access destination bucket |
| S3 Bucket Name | `bucketName` | The name of the destination S3 bucket                                    |
| Bucket Path    | `bucketPath` | Optional path prefix within the bucket where logs will be stored         |

**Authentication Flow:**

1. WorkOS uses AWS Security Token Service (STS) to assume a role in the destination AWS account
2. The role must be configured to trust WorkOS' AWS account ID (`workosAccountId`) as an external trusted entity
3. The role must require an External ID (`externalId`) that matches the unique value provided by WorkOS
4. The role must have an attached IAM policy granting `s3:PutObject` permissions on the bucket (and optional path prefix)
5. WorkOS receives temporary credentials from STS and uses them to upload audit log events to the destination S3 bucket

**IAM Policy Requirements:**

The IAM role must include a policy that allows `s3:PutObject` actions on the destination bucket. The policy resource should target destination bucket and optional path prefix: `arn:aws:s3:::bucket-name/optional-path/*`.

Example policy that you need to create in the destination AWS account:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "LogStreamBucketPolicy",
      "Effect": "Allow",
      "Action": ["s3:PutObject"],
      "Resource": ["arn:aws:s3:::bucket-name/optional-path/*"]
    }
  ]
}
```

### Google Cloud Storage

Events are stored as individual JSON files using Google Cloud Storage's S3-compatible API.

| Property            | Description                                         |
| ------------------- | --------------------------------------------------- |
| File Format         | Individual JSON files per event (same format as S3) |
| File Naming Pattern | `{timestamp}_{keySuffix}.json`                      |

**Example File Content:**

```json
{
  "id": "01HY123456ABCDEFGHIJK",
  "action": "user.signed_in",
  "targets": [
    {
      "id": "user_123",
      "type": "user"
    }
  ],
  "actor": {
    "id": "user_456",
    "type": "user"
  },
  "context": {
    "location": "192.168.1.1",
    "user_agent": "Chrome/91.0"
  },
  "occurred_at": "2024-01-15T10:30:00.000Z"
}
```

**Configuration:**

- Access Key ID and Secret Access Key are required when configuring a log stream to GCS
- GCS bucket with S3-compatible access

### Microsoft Sentinel

Events are sent to Microsoft Sentinel via the Azure Monitor Logs Ingestion API.

**Example Payload:**

```json
[
  {
    "TimeGenerated": "2024-01-15T10:30:00.000Z",
    "id": "01HY123456ABCDEFGHIJK",
    "event_type": "user.signed_in",
    "organization_id": "org_01ABC123",
    "data": {
      "id": "01HY123456ABCDEFGHIJK",
      "action": "user.signed_in",
      "targets": [
        {
          "id": "user_123",
          "type": "user"
        }
      ],
      "actor": {
        "id": "user_456",
        "type": "user"
      },
      "context": {
        "location": "192.168.1.1",
        "user_agent": "Chrome/91.0"
      },
      "occurred_at": "2024-01-15T10:30:00.000Z"
    }
  }
]
```

**Configuration:**

| Property                     | Description                                            |
| ---------------------------- | ------------------------------------------------------ |
| Tenant ID                    | Azure Active Directory tenant ID                       |
| Client ID                    | Application (client) ID from Azure AD app registration |
| Client Secret                | Client secret from Azure AD app registration           |
| Data Collection Endpoint URL | The URL of the Data Collection Endpoint (DCE)          |
| Data Collection Rule ID      | The immutable ID of the Data Collection Rule (DCR)     |
| Stream Name                  | The name of the Logs Stream                            |

### Generic HTTPS

Events are sent to custom HTTP endpoints with configurable authentication and format options.

**JSON Format Example:**

```json
[
  {
    "event": {
      "id": "01HY123456ABCDEFGHIJK",
      "action": "user.signed_in",
      "targets": [
        {
          "id": "user_123",
          "type": "user"
        }
      ],
      "actor": {
        "id": "user_456",
        "type": "user"
      },
      "context": {
        "location": "192.168.1.1",
        "user_agent": "Chrome/91.0"
      },
      "occurred_at": "2024-01-15T10:30:00.000Z"
    },
    "keySuffix": "abc123def456",
    "timestamp": "2024-01-15T10:30:00.123Z",
    "source": "team-name"
  }
]
```

**NDJSON Format Example:**

```json
{"event":{"id":"01HY123456ABCDEFGHIJK","action":"user.signed_in",...},"keySuffix":"abc123def456","timestamp":"2024-01-15T10:30:00.123Z"}
```

**Configuration:**

- Custom HTTP endpoint
- Configurable authentication headers
- Support for JSON or NDJSON formats
- Content-Type handling (application/json or application/x-ndjson)

## Stream States and Management

Audit log streams can be in one of four states that determine their operational status:

### Stream States

| State        | Description                                               |
| ------------ | --------------------------------------------------------- |
| **Active**   | Stream is functioning normally and delivering events      |
| **Inactive** | Stream is incomplete, manually disabled or paused         |
| **Error**    | Stream encountered a retry-able error and will be retried |
| **Invalid**  | Stream has invalid credentials or configuration           |

### State Transitions

Streams automatically transition between states based on delivery outcomes:

- **Active → Error**: When a retry-able error occurs during event delivery
- **Active → Invalid**: When authentication or authorization fails
- **Error → Active**: When retry succeeds after a previous error
- **Invalid → Active**: When credentials are fixed and validation succeeds
- **Any → Inactive**: When manually disabled through Dashboard or Admin Portal

### Updating Stream Configuration

Stream configurations can be updated through:

1. **WorkOS Dashboard**: Navigate to the organization and modify the log stream configuration
2. **Admin Portal**: Generate a setup link for the organization's IT admin to update settings
