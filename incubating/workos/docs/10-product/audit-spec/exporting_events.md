# Exporting Events

## Exporting Events

You may need to export Audit Log Events in large chunks. WorkOS supports exporting events as CSV files through both the Dashboard and API.

Exports are scoped to a single organization within a specified date range. Events from the past three months can be included in the export. You may define additional filters such as `actions`, `actors`, and `targets`.

### Creating an export through the Dashboard

Exports can be manually created under the Organization page when viewing Audit Log Events by selecting "Export CSV" from the "Actions" dropdown. Set your filters and select "Generate CSV file".

![A screenshot showing how to generate an Audit Log export in the WorkOS Dashboard.](https://images.workoscdn.com/images/a5386939-652f-4cbb-aa88-7159e2ffc1dd.png?auto=format\&fit=clip\&q=50)

### Creating an export through the API

#### Create an Export

```js
import { WorkOS } from '@workos-inc/node';

const workos = new WorkOS('WORKOS_API_KEY_PLACEHOLDER');

const auditLogExport = await workos.auditLogs.createExport({
  organizationId: 'org_01EHWNCE74X7JSDV0X3SZ3KJNY',
  rangeStart: new Date('2022-08-31T15:51:23.604Z'),
  rangeEnd: new Date('2022-08-31T15:51:23.604Z'),
});

// Use auditLogExport.id for fetching export at a later time
```

```rb
require "workos"

WorkOS.configure do |config|
  config.key = "WORKOS_API_KEY_PLACEHOLDER"
end

audit_log_export = WorkOS::AuditLogs.create_export(
  organization: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
  range_start: "2022-06-22T15:04:19.704Z",
  range_end: "2022-08-22T15:04:19.704Z"
)

# Use audit_log_export.id for fetching export at a later time
```

```py
from workos import WorkOSClient

workos_client = WorkOSClient(
    api_key="WORKOS_API_KEY_PLACEHOLDER", client_id="WORKOS_CLIENT_ID_PLACEHOLDER"
)

workos_client.audit_logs.create_export(
    organization_id="org_01EHWNCE74X7JSDV0X3SZ3KJNY",
    range_start="2022-08-31T15:51:23.604Z",
    range_end="2022-08-31T15:51:23.604Z",
)
```

```go
package main

import (
	"context"

	"github.com/workos/workos-go/v3/pkg/auditlogs"
)

func main() {
	auditlogs.SetAPIKey("WORKOS_API_KEY_PLACEHOLDER")

	export, err := auditlogs.CreateExport(context.Background(), auditlogs.CreateExportOpts{
		OrganizationID: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
		RangeStart:     "2022-08-31T15:51:23.604Z",
		RangeEnd:       "2022-08-31T15:51:23.604Z",
	})

	// Use auditLogExport.ID for fetching export at a later time
}
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogExport = $auditLogs->createExport(
    organizationId: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
    rangeStart: "2022-06-31T15:51:23.604Z",
    rangeEnd: "2022-08-31T15:51:23.604Z"
);

// Use $auditLogExport.id for fetching export at a later time
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogExport = $auditLogs->createExport(
    organizationId: "org_01EHWNCE74X7JSDV0X3SZ3KJNY",
    rangeStart: "2022-06-31T15:51:23.604Z",
    rangeEnd: "2022-08-31T15:51:23.604Z"
);

// Use $auditLogExport.id for fetching export at a later time
```

```java
import com.workos.WorkOS;
import com.workos.auditlogs.AuditLogsApi.CreateAuditLogExportOptions;
import com.workos.auditlogs.models.AuditLogExport;
import java.util.Date;

WorkOS workos = new WorkOS("WORKOS_API_KEY_PLACEHOLDER");

Date rangeStart = new Date();
rangeStart.setMonth(rangeStart.getMonth() - 3);

Date rangeEnd = new Date();

CreateAuditLogExportOptions options = CreateAuditLogExportOptions.builder()
                                          .organizationId("org_123")
                                          .rangeStart(rangeStart)
                                          .rangeEnd(rangeEnd)
                                          .build();

AuditLogExport auditLogExport = workos.auditLogs.createExport(options);

// Use auditLogExport.id for fetching export at a later time
```

```cs
WorkOS.SetApiKey("WORKOS_API_KEY_PLACEHOLDER");

var auditLogsService = new AuditLogsService();

var options = new CreateAuditLogExportOptions() {
    OrganizationId = "org_123",
    RangeStart = DateTime.Now.AddMonths(-3),
    RangeEnd = DateTime.Now,
};

var auditLogExport = await auditLogsService.CreateExport(options);

// Use auditLogExport.Id for fetching export at a later time
```

Once the export has been created, fetch the export at a later time to access the `url` of the generated CSV file.

> The URL will expire after 10 minutes. If the export is needed again at a later time, refetching the export will regenerate the URL.

#### Fetch Export

```js
import { WorkOS } from '@workos-inc/node';

const workos = new WorkOS('WORKOS_API_KEY_PLACEHOLDER');

const auditLogExport = await workos.auditLogs.getExport(
  'audit_log_export_01GBT9P815WPET6H8K0XHBACGS',
);

// auditLogExport.state `pending` or `ready`
// auditLogExport.url available once `state` is `ready`
```

```rb
require "workos"

WorkOS.configure do |config|
  config.key = "WORKOS_API_KEY_PLACEHOLDER"
end

audit_log_export = WorkOS::AuditLogs.get_export(
  id: "audit_log_export_01GBT9P815WPET6H8K0XHBACGS"
)
```

```py
from workos import WorkOSClient

workos_client = WorkOSClient(
    api_key="WORKOS_API_KEY_PLACEHOLDER", client_id="WORKOS_CLIENT_ID_PLACEHOLDER"
)

audit_log_export = workos_client.audit_logs.get_export(
    audit_log_export_id="audit_log_export_01GBT9P815WPET6H8K0XHBACGS"
)

# audit_log_export.state "pending" or "ready"
# audit_log_export.url available once "state" is "ready"
```

```go
package main

import (
	"context"

	"github.com/workos/workos-go/v3/pkg/auditlogs"
)

func main() {
	auditlogs.SetAPIKey("WORKOS_API_KEY_PLACEHOLDER")

	export, err := auditlogs.GetExport(context.Background(), auditlogs.GetExportOpts{
		ExportID: "audit_log_export_01GBT9P815WPET6H8K0XHBACGS",
	})

	// export.State `pending` or `ready`
	// export.Url available once `State` is `ready`
}
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogExport = $auditLogs->getExport(
    "audit_log_export_01GBT9P815WPET6H8K0XHBACGS"
);

// $auditLogExport.state `pending` or `ready`
// $auditLogExport.url available once `state` is `ready`
```

```php
<?php

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$auditLogs = new WorkOS\AuditLogs();

$auditLogExport = $auditLogs->getExport(
    "audit_log_export_01GBT9P815WPET6H8K0XHBACGS"
);

// $auditLogExport.state `pending` or `ready`
// $auditLogExport.url available once `state` is `ready`
```

```java
import com.workos.WorkOS;
import com.workos.auditlogs.models.AuditLogExport;

WorkOS workos = new WorkOS("WORKOS_API_KEY_PLACEHOLDER");

AuditLogExport auditLogExport =
    workos.auditLogs.getExport("audit_log_export_01GBT9P815WPET6H8K0XHBACGS");
```

```cs
WorkOS.SetApiKey("WORKOS_API_KEY_PLACEHOLDER");

var auditLogsService = new AuditLogsService();

var auditLogExport = await auditLogsService.GetExport("audit_log_export_01GBT9P815WPET6H8K0XHBACGS");
```

If the `state` of the export is still `pending`, poll the export until it is ready for download.
