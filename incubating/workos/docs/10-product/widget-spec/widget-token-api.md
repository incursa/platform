# Widgets

Widgets are React components that provide complete functionality for common enterprise app workflows.

## Generate a Widget token

Generate a widget token scoped to an organization and user with the specified scopes.

#### Request

```bash
curl --request POST \
  --url https://api.workos.com/widgets/token \
  --header "Authorization: Bearer WORKOS_API_KEY_PLACEHOLDER" \
  -d organization_id="org_01EHZNVPK3SFK441A1RGBFSHRT" \
  -d user_id="usr_01EHZNVPK3SFK441A1RGBFSHRT" \
  -d scopes="widgets:users-table:manage"
```

```js
import { WorkOS } from '@workos-inc/node';

const workos = new WorkOS('WORKOS_API_KEY_PLACEHOLDER');

const { token } = await workos.widgets.getToken({
  organizationId: 'org_01EHZNVPK3SFK441A1RGBFSHRT',
  userId: 'user_01EHZNVPK3SFK441A1RGBFSHRT',
  scopes: ['widgets:users-table:manage'],
});
```

```rb
require "workos"

WorkOS.configure do |config|
  config.key = "WORKOS_API_KEY_PLACEHOLDER"
end

token = WorkOS::Widgets.get_token(
  organization_id: "org_01EHZNVPK3SFK441A1RGBFSHRT",
  user_id: "user_01EHZNVPK3SFK441A1RGBFSHRT",
  scopes: ["widgets:users-table:manage"]
)
```

```py
from workos import WorkOSClient

workos_client = WorkOSClient(
    api_key="WORKOS_API_KEY_PLACEHOLDER", client_id="WORKOS_CLIENT_ID_PLACEHOLDER"
)

token_response = workos_client.widgets.get_token(
    organization_id="org_01EHZNVPK3SFK441A1RGBFSHRT",
    user_id="user_01EHZNVPK3SFK441A1RGBFSHRT",
    scopes=["widgets:users-table:manage"],
)
```

```go
package main

import (
	"context"

	"github.com/workos/workos-go/v4/pkg/widgets"
)

func main() {
	widgets.SetAPIKey("WORKOS_API_KEY_PLACEHOLDER")

	token, err := widgets.GetToken(
		context.Background(),
		widgets.GetTokenOpts{
			OrganizationID: "org_01EHZNVPK3SFK441A1RGBFSHRT",
			UserID:         "user_01EHZNVPK3SFK441A1RGBFSHRT",
			Scopes:         []widgets.WidgetScope{widgets.UsersTableManage},
		},
	)
}
```

```php
<?php

use WorkOS\Resource\WidgetScope;

WorkOS\WorkOS::setApiKey("WORKOS_API_KEY_PLACEHOLDER");

$widgets = new WorkOS\Widgets();
$token_response = $widgets->getToken(
    organization_id: "org_01EHZNVPK3SFK441A1RGBFSHRT",
    user_id: "user_01EHZNVPK3SFK441A1RGBFSHRT",
    scopes: [WidgetScope::UsersTableManage]
);
```

```java
import com.workos.WorkOS;
import com.workos.widgets.WidgetsApi.GetTokenOptions;
import com.workos.widgets.models.WidgetScope;
import com.workos.widgets.models.WidgetTokenResponse;

WorkOS workos = new WorkOS("WORKOS_API_KEY_PLACEHOLDER");

String organizationId = "org_01EHZNVPK3SFK441A1RGBFSHRT";
String userId = "user_01EHZNVPK3SFK441A1RGBFSHRT";
GetTokenOptions options = GetTokenOptions.builder()
                              .organizationID(organizationId)
                              .userID(userId)
                              .scopes(Arrays.asList(WidgetScope.WidgetsUsersTableManage))
                              .build();

Link response = workos.widgets.getToken(options);
String token = response.token;
```

#### Response

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### POST /widgets/token

#### Parameters

| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| `organization_id` | string | Yes | An [Organization](/reference/organization) identifier. |
| `user_id` | string | No | A [User](/reference/authkit/user) identifier. |
| `scopes` | "widgets:users-table:manage"[] | Yes | Scopes to include in the widget token. If a user_id is provided, the requested scopes will be also be restricted to the permissions that the user has in the requested organization. |

#### Returns

| Field | Type | Description |
| --- | --- | --- |
| `token` | string | An ephemeral token to access WorkOS widgets. |
