# Incursa.Integrations.WorkOS.Access

`Incursa.Integrations.WorkOS.Access` is the WorkOS-facing adapter for `Incursa.Platform.Access`. It now covers both:

- synchronization of WorkOS organization/membership data into the provider-neutral access model
- server-side authentication/session integration for custom UI login flows backed by WorkOS User Management

## What It Owns

- translation from WorkOS organization memberships into local scope memberships and role assignments
- WorkOS-specific alias defaults for organization external links and role lookup
- deterministic provider-managed identifiers for membership and assignment reconciliation
- optional reconciliation work-item hooks when provider snapshots should be revisited
- WorkOS auth API calls for password, magic auth, email verification, TOTP, organization selection, code exchange, refresh, and sign-out
- issuer-aware JWT validation with cached JWKS lookup
- WorkOS-specific options and error/challenge mapping

## What It Does Not Own

- the canonical permission or role registry
- auth middleware, claims enrichment, or WorkOS session handling
- direct source-of-truth access storage outside `Incursa.Platform.Access`

## When To Use It

Use this package when your application keeps its canonical authorization state in `Incursa.Platform.Access` but needs to synchronize or interpret WorkOS organization membership data.

## Usage

Register the core access capability first, then register the WorkOS adapter:

```csharp
services.AddAccess(registry =>
{
    registry.AddPermission(new AccessPermissionDefinition(
        new AccessPermissionId("tenant.read"),
        "Read tenant"));

    registry.AddRole(new AccessRoleDefinition(
        new AccessRoleId("organization-admin"),
        "Organization administrator",
        [new AccessPermissionId("tenant.read")],
        providerAliases: new Dictionary<string, string>
        {
            [WorkOsAccessDefaults.RoleAliasKey] = "org_admin",
        }));
});

services.AddWorkOsAccess();
```

For custom UI login/signup/verification/MFA flows, register the WorkOS authentication integration separately:

```csharp
services.AddWorkOsAuthentication(options =>
{
    options.ClientId = builder.Configuration["WorkOs:ClientId"]!;
    options.ClientSecret = builder.Configuration["WorkOs:ClientSecret"]!;
    options.ApiKey = builder.Configuration["WorkOs:ApiKey"]!;
    options.AuthApiBaseUrl = builder.Configuration["WorkOs:AuthApiBaseUrl"];
    options.Issuer = builder.Configuration["WorkOs:Issuer"];
    options.ExpectedAudiences = [options.ClientId];
});
```

Then consume the provider-neutral app-facing service:

```csharp
var outcome = await authenticationService.SignInWithPasswordAsync(
    new AccessPasswordSignInRequest(email, password),
    cancellationToken);
```

Handle `AccessAuthenticationOutcome` explicitly:

- success: issue the local app cookie/session
- challenge: render the next custom UI step using the challenge payload
- failure: return a validation/authentication error to the caller

`SignOutAsync` clears the local app session in the ASP.NET Core adapter and, when a WorkOS session id is available, this package revokes the remote WorkOS session and can return a WorkOS logout URL.

## Configuration

Required:

- `ClientId`
- at least one of `ClientSecret` or `ApiKey`

Optional:

- `ApiBaseUrl` defaults to `https://api.workos.com`
- `AuthApiBaseUrl` for a custom WorkOS auth domain
- `Issuer` to override token issuer validation
- `ExpectedAudiences` to validate access token audiences
- `RequestTimeout`
- `JwksCacheDuration`

WorkOS roles are resolved through provider aliases on the local registry definitions; the adapter does not maintain a second role registry.

Existing scope roots can carry a WorkOS organization link up front:

```csharp
var scopeRoot = new ScopeRoot(
    new ScopeRootId("org-123"),
    ScopeRootKind.Organization,
    "Contoso",
    externalLinks:
    [
        new ExternalLink(
            new ExternalLinkId("workos-org-123"),
            WorkOsAccessDefaults.ProviderName,
            "org_01H...",
            WorkOsAccessDefaults.OrganizationResourceType),
    ]);
```

If a matching scope root does not exist, the adapter can materialize an organization scope root from the WorkOS organization id during synchronization.

## Consistency Model

- local access records remain the source of truth
- WorkOS-managed memberships and assignments are reconciled by deterministic local identifiers
- scope-root creation, membership updates, and projection updates still follow the core package's eventual-consistency model
- broader WorkOS claim/session middleware and application-specific organization-to-tenant collapse patterns now live in the public `Incursa.Integrations.WorkOS*` packages

## Related Packages

- `Incursa.Platform.Access`
- `Incursa.Platform.Access.AspNetCore`
- `Incursa.Integrations.WorkOS`
- `Incursa.Integrations.WorkOS.AspNetCore`
