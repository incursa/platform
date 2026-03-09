# API Keys Widget

![API Keys widget screenshot](https://images.workoscdn.com/images/13e0893f-eb84-43f1-9467-2598567538f3.png?auto=format\&fit=clip\&q=50)

The `<ApiKeys />` widget allows an admin to manage API keys in an organization. Admins can create API keys with specific permissions, view details of existing API keys, and revoke API keys, all within the widget.

In order to use the API Keys widget, a user must have a role that has the `widgets:api-keys:manage` permission.

#### Widget Token

```js
import { ApiKeys, WorkOsWidgets } from '@workos-inc/widgets';

/**
 * @param {string} authToken - A widget token that was fetched in your backend. See the
 * "Tokens" section of this guide for details on how to generate the token
 */
export function ApiKeysPage({ authToken }) {
  return (
    <WorkOsWidgets>
      <ApiKeys authToken={authToken} />
    </WorkOsWidgets>
  );
}
```

#### Access Token

```js
import { useAuth } from '@workos-inc/authkit-react';
import { ApiKeys, WorkOsWidgets } from '@workos-inc/widgets';

export function ApiKeysPage() {
  const { isLoading, user, getAccessToken } = useAuth();
  if (isLoading) {
    return '...';
  }
  if (!user) {
    return 'Logged in user is required';
  }

  return (
    <WorkOsWidgets>
      <ApiKeys authToken={getAccessToken} />
    </WorkOsWidgets>
  );
}
```

## API Reference
