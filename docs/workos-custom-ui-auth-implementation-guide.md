# WorkOS Custom UI Authentication Implementation Guide

## Purpose

This document is a handoff guide for implementing a full ASP.NET Core authentication experience on top of the WorkOS-backed authentication backend that now exists in this repository.

The intended audience is the engineer or team building the website, pages, endpoints, controllers, and integration glue that sit above the platform libraries.

This guide answers:

- what the backend now supports
- what pages and UI screens are required
- how an ASP.NET Core app should wire it in
- what WorkOS configuration is required
- what is still missing if you want a complete self-service authentication product

## Important Scope Boundary

The backend added in this repo supports:

- redirect and authorization-code exchange flows
- direct email/password sign-in
- magic auth start and completion
- email verification completion
- TOTP MFA enrollment and challenge completion
- organization selection completion
- session refresh with refresh-token rotation handling
- local sign-out plus remote WorkOS session revoke/logout URL support
- access-token validation against JWKS
- provider-neutral session and access-context handling for ASP.NET Core

The backend does not currently provide first-class app-facing methods for:

- self-service sign-up / create-user
- forgot password / request password reset
- reset password with token
- account security management such as change password or reset MFA settings
- custom email-verification email delivery via a `GetEmailVerification` app-facing API

That means:

- if your product only needs existing users to sign in, complete challenges, and sign out, the current backend is enough
- if your product needs full self-service onboarding and recovery, you need additional backend work beyond what is already implemented here

## Short Answer: Is A WorkOS API Key Alone Enough?

No.

For a production-ready custom UI integration, assume you need:

- `ClientId`
- `ClientSecret`
- `ApiKey`

And usually also:

- configured redirect URI(s)
- configured sign-out redirect
- selected/allowed authentication methods in the WorkOS Dashboard
- issuer and custom auth-domain settings if using a custom auth domain
- email delivery configuration if you want branded emails

In this repo's implementation:

- `ClientSecret` is used for the grant-style authentication exchanges
- `ApiKey` is used for WorkOS management-style calls such as Magic Auth creation, factor enrollment, and session revoke
- `ClientId` is used to build authorization requests and validate token audiences

Even though the options model has some fallback behavior, the practical guidance is:

- configure all three: `ClientId`, `ClientSecret`, and `ApiKey`

## Recommended Delivery Model

Split the work into three layers.

### Layer A: Already Implemented In This Repo

Use the platform libraries for:

- auth API orchestration
- WorkOS challenge normalization
- JWT validation
- cookie/session persistence
- current access-context resolution

### Layer B: App-Specific ASP.NET Core Integration

The consuming application must build:

- pages/screens
- controller/page-model/minimal-api endpoints
- redirects between auth steps
- view models and validation
- success/failure messaging
- return URL handling

### Layer C: Optional Additional Backend Work

If you want complete self-service auth, add backend support for:

- sign-up
- password reset request
- password reset completion
- settings/security management
- custom email verification retrieval/delivery if WorkOS-hosted emails are disabled

## Current Backend Surface In This Repo

The app-facing provider-neutral service is `IAccessAuthenticationService`.

Primary operations:

- `CreateAuthorizationUrlAsync(...)`
- `ExchangeCodeAsync(...)`
- `SignInWithPasswordAsync(...)`
- `BeginMagicAuthAsync(...)`
- `CompleteMagicAuthAsync(...)`
- `CompleteEmailVerificationAsync(...)`
- `EnrollTotpAsync(...)`
- `CompleteTotpAsync(...)`
- `CompleteOrganizationSelectionAsync(...)`
- `RefreshAsync(...)`
- `SignOutAsync(...)`

Expected auth results are typed, not exception-driven:

- `AccessAuthenticationSucceeded`
- `AccessAuthenticationChallengeRequired`
- `AccessAuthenticationFailed`

Challenge kinds:

- `EmailVerificationRequired`
- `MfaEnrollmentRequired`
- `MfaChallengeRequired`
- `OrganizationSelectionRequired`
- `IdentityLinkingRequired`
- `ProviderChallengeRequired`

ASP.NET Core helpers:

- `IAccessAuthenticationTicketService`
- `IAccessSessionStore`
- `ICurrentAccessContextAccessor`
- `AddAccessCookieAuthentication(...)`
- `AddWorkOsAccessAspNetCore(...)`
- `AddWorkOsCustomUiAuthentication(...)`

## What The ASP.NET App Should Register

The simplest integration path for the consuming app is:

```csharp
services.AddWorkOsCustomUiAuthentication(
    configureAuth: options =>
    {
        options.ClientId = builder.Configuration["WorkOs:ClientId"]!;
        options.ClientSecret = builder.Configuration["WorkOs:ClientSecret"]!;
        options.ApiKey = builder.Configuration["WorkOs:ApiKey"]!;
        options.AuthApiBaseUrl = builder.Configuration["WorkOs:AuthApiBaseUrl"];
        options.Issuer = builder.Configuration["WorkOs:Issuer"];
        options.ExpectedAudiences = [builder.Configuration["WorkOs:ClientId"]!];
    },
    configureAccess: options =>
    {
        options.ScopeRootExternalLinkProvider = "workos";
        options.ScopeRootExternalLinkResourceType = "organization";
    },
    configureCookie: options =>
    {
        options.AuthenticationScheme = "Access";
    });
```

The app should then inject:

- `IAccessAuthenticationService`
- `IAccessAuthenticationTicketService`
- `ICurrentAccessContextAccessor`

## Minimum WorkOS Dashboard / Environment Configuration

Before any UI work, confirm the following in WorkOS.

### Required

- a WorkOS environment exists for the app
- `ClientId` is provisioned
- `ClientSecret` is provisioned
- `ApiKey` is provisioned
- at least one redirect URI is configured
- a sign-out redirect URI is configured

### Strongly Recommended

- enable only the authentication methods you actually want to expose
- configure a custom auth domain for production
- set `AuthApiBaseUrl` and `Issuer` in the app when a custom auth domain is used
- configure an email sending domain or custom email provider if WorkOS should send your auth emails

### Optional Depending On Product

- enable email/password
- enable Magic Auth
- enable Google / Microsoft OAuth
- enable SSO
- enable MFA
- configure verified domains / org policies / JIT membership behavior

## Recommended Route Map

These routes are the recommended app-level surface for a complete custom-UI experience.

### Required For The Current Backend

- `GET /auth/sign-in`
- `POST /auth/sign-in/password`
- `GET /auth/callback`
- `GET /auth/magic`
- `POST /auth/magic`
- `GET /auth/magic/verify`
- `POST /auth/magic/verify`
- `GET /auth/verify-email`
- `POST /auth/verify-email`
- `GET /auth/mfa/setup`
- `POST /auth/mfa/setup`
- `GET /auth/mfa/verify`
- `POST /auth/mfa/verify`
- `GET /auth/organizations/select`
- `POST /auth/organizations/select`
- `POST /auth/sign-out`

### Optional But Recommended App Pages

- `GET /auth/error`
- `GET /auth/unauthorized`
- `GET /auth/session-expired`

### Additional Routes If You Want A Full Auth Product

These require new backend work beyond what currently exists.

- `GET /auth/sign-up`
- `POST /auth/sign-up`
- `GET /auth/forgot-password`
- `POST /auth/forgot-password`
- `GET /auth/reset-password`
- `POST /auth/reset-password`
- `GET /settings/security`
- `POST /settings/security/password`
- `POST /settings/security/mfa/reset`

## Page-By-Page UI Guidance

## 1. Sign-In Page

### Purpose

The main entry point into authentication.

### UI Elements

- email input
- password input
- submit button for password sign-in
- link or tab for Magic Auth
- buttons for redirect-based providers if enabled:
  - Google
  - Microsoft
  - enterprise SSO
- optional remember/continue text
- link to sign-up if self-service sign-up exists
- link to forgot password if password reset exists

### Backend Calls

- email/password form posts to `SignInWithPasswordAsync(...)`
- redirect provider buttons call `CreateAuthorizationUrlAsync(...)` and redirect
- Magic Auth entry links to the Magic Auth page

### Success Handling

- if success: issue local cookie with `IAccessAuthenticationTicketService.SignInAsync(...)` and redirect to return URL
- if challenge: redirect to the appropriate challenge screen
- if failure: redisplay page with a safe generic auth error

### Notes

- the page must preserve a validated return URL
- never echo whether the email exists unless product requirements explicitly allow it

## 2. Redirect Provider Initiation

### Purpose

Start Google/Microsoft/SSO/AuthKit-style redirect flows.

### UI Elements

- usually buttons on the sign-in page, not a separate screen

### Backend Calls

- `CreateAuthorizationUrlAsync(new AccessRedirectAuthorizationRequest(...))`

### Required Inputs

- redirect URI
- optional provider
- optional connection id
- optional organization id
- optional state
- optional PKCE challenge if you choose to use it

### Output

- redirect the browser to the returned URL

## 3. Callback Endpoint / Page

### Purpose

Receive the authorization code from WorkOS and exchange it for a session.

### Route

- `GET /auth/callback`

### Query Parameters

- `code`
- `state`
- potentially provider-specific error values

### Backend Calls

- `ExchangeCodeAsync(new AccessCodeExchangeRequest(...))`

### Success Handling

- if success: issue local cookie and redirect
- if challenge: redirect to verification, MFA, or org selection
- if failure: redirect to `/auth/error`

### Notes

- validate the anti-forgery / correlation `state`
- if you use PKCE, persist and replay the verifier

## 4. Magic Auth Request Page

### Purpose

Let the user request a magic code.

### Route

- `GET /auth/magic`
- `POST /auth/magic`

### UI Elements

- email input
- submit button
- info text explaining a code will be sent

### Backend Calls

- `BeginMagicAuthAsync(new AccessMagicAuthStartRequest(email) { ReturnCode = false })`

### Success Handling

- redirect to `/auth/magic/verify?email=...`
- show "check your email"

### If App-Owned Email Delivery Is Desired

The current backend can return the code for Magic Auth if `ReturnCode = true`.

That means:

- custom email delivery for Magic Auth is possible now
- if you do this, your app must send the email itself
- the code must never be exposed to browser JavaScript

## 5. Magic Auth Verification Page

### Purpose

Accept the one-time Magic Auth code and turn it into a session.

### Route

- `GET /auth/magic/verify`
- `POST /auth/magic/verify`

### UI Elements

- code input
- optional email display
- resend code action

### Backend Calls

- `CompleteMagicAuthAsync(new AccessMagicAuthCompletionRequest(code, metadata))`

### Success Handling

- if success: issue cookie and redirect
- if challenge: redirect to verification, MFA, or org selection
- if failure: show invalid/expired code state

## 6. Email Verification Page

### Purpose

Handle `EmailVerificationRequired` challenges.

### Route

- `GET /auth/verify-email`
- `POST /auth/verify-email`

### Required State

Persist these server-side between requests:

- `PendingAuthenticationToken`
- `Email`
- `EmailVerificationId` if present

### UI Elements

- read-only email display
- code input
- submit button
- resend email action if your product supports it

### Backend Calls

- `CompleteEmailVerificationAsync(new AccessEmailVerificationRequest(...))`

### Important Limitation

The current backend can complete email verification, but it does not yet expose an app-facing API for retrieving email-verification details for custom email delivery.

That means:

- if WorkOS sends the verification email, the current backend is enough
- if your app wants to send its own email verification emails, additional backend work is still needed

## 7. MFA Enrollment Page

### Purpose

Handle `MfaEnrollmentRequired`.

### Route

- `GET /auth/mfa/setup`
- `POST /auth/mfa/setup`

### Required State

- `PendingAuthenticationToken`
- `Email`

### UI Elements

- QR code region
- manual secret fallback
- authenticator-app instructions
- TOTP code input
- continue button

### Backend Calls

Step 1:

- call `EnrollTotpAsync(new AccessTotpEnrollmentRequest(issuer, user))`

Step 2:

- call `CompleteTotpAsync(new AccessTotpCompletionRequest(...))`

### Notes

- the current backend exposes enrollment separately from challenge completion
- the app should use a stable issuer label such as your product name
- the app should use the user’s email as the TOTP user label

## 8. MFA Verification Page

### Purpose

Handle `MfaChallengeRequired`.

### Route

- `GET /auth/mfa/verify`
- `POST /auth/mfa/verify`

### Required State

- `PendingAuthenticationToken`
- `AuthenticationChallengeId` if present
- `AuthenticationFactorId` if present

### UI Elements

- code input
- submit button
- contextual hint like "Enter the code from your authenticator app"

### Backend Calls

- `CompleteTotpAsync(new AccessTotpCompletionRequest(...))`

### Notes

- the backend will create a TOTP challenge if one does not already exist and a factor id is supplied

## 9. Organization Selection Page

### Purpose

Handle `OrganizationSelectionRequired` for users with multiple organizations.

### Route

- `GET /auth/organizations/select`
- `POST /auth/organizations/select`

### Required State

- `PendingAuthenticationToken`
- list of organizations from the challenge payload

### UI Elements

- selectable list of organizations
- continue button
- optional organization name and avatar if your app has branding data

### Backend Calls

- `CompleteOrganizationSelectionAsync(new AccessOrganizationSelectionRequest(...))`

### Notes

- do not trust a posted organization id unless it was in the original challenge payload

## 10. Sign-Out Endpoint

### Purpose

Clear local session state and revoke the remote WorkOS session when possible.

### Route

- `POST /auth/sign-out`

### Backend Calls

- `IAccessAuthenticationTicketService.SignOutAsync(...)`

### Behavior

- clears the local auth cookie
- clears the local serialized session
- if a WorkOS session id is present, revokes the remote session
- may return a WorkOS logout URL

### UX Guidance

- usually redirect to a signed-out landing page or `/auth/sign-in`

## Pages Not Strictly Required For MVP But Usually Expected

## 11. Authentication Error Page

Show:

- expired code
- invalid callback state
- unsupported auth method
- generic provider error

Do not expose raw provider payloads.

## 12. Unauthorized / Access Denied Page

Useful after login if the user is authenticated but lacks required permissions or tenant membership.

## 13. Session Expired Page

Useful when refresh fails or the local session cannot be restored.

## Additional Backend Work Recommended For A Full Product

## A. Sign-Up

If you want users to create accounts directly from your UI, add backend support for:

- create user / sign-up
- post-sign-up email verification behavior
- invite acceptance if applicable

Suggested UI:

- `GET /auth/sign-up`
- `POST /auth/sign-up`

Suggested fields:

- email
- password
- confirm password
- optional organization / invite token context
- acceptance of terms if required by the product

## B. Forgot Password And Reset Password

WorkOS supports password reset, but the current repo backend does not yet expose it.

Per current WorkOS docs:

- a password reset token can be created
- resetting the password revokes active sessions
- password reset completion can also verify email ownership if needed

Suggested UI:

- `GET /auth/forgot-password`
- `POST /auth/forgot-password`
- `GET /auth/reset-password?token=...`
- `POST /auth/reset-password`

Suggested backend work to add later:

- `RequestPasswordResetAsync(email)`
- `ResetPasswordAsync(token, newPassword)`

## C. Security Settings Page

Once the user is signed in, a product usually needs a security/settings page for:

- change password
- reset/reconfigure MFA
- session/device management

None of that is currently exposed in the provider-neutral auth API in this repo.

## ASP.NET Core Implementation Guidance

## Recommended Architecture

Keep the UI thin.

- Pages or controllers own input validation and navigation
- `IAccessAuthenticationService` owns auth operations
- `IAccessAuthenticationTicketService` owns local cookie issuance and local sign-out
- `ICurrentAccessContextAccessor` owns current-user/current-org resolution

## Recommended Controller / Endpoint Pattern

Each POST handler should:

1. validate form input
2. call the appropriate `IAccessAuthenticationService` method
3. branch on `AccessAuthenticationOutcome`
4. on success, call `IAccessAuthenticationTicketService.SignInAsync(...)`
5. on challenge, redirect to the correct challenge page and persist the challenge state server-side
6. on failure, re-render the current page with a safe message

## Strong Recommendation: Centralize Outcome Handling

Create one internal helper in the website such as:

- `AuthenticationOutcomeExecutor`
- `AuthenticationFlowRouter`
- `HandleOutcomeAsync(...)`

That helper should:

- sign the user in on success
- redirect to the correct page based on `AccessChallengeKind`
- persist challenge state between requests
- map `AccessAuthenticationFailed` into consistent UI errors

This prevents each page from re-implementing challenge routing.

## Session Persistence Guidance

The default ASP.NET Core implementation uses:

- encrypted cookie storage
- HttpOnly cookies
- server-side sign-in/sign-out helpers

That is a good default for a server-rendered or hybrid ASP.NET Core app.

If later needed, you can replace `IAccessSessionStore` with:

- database-backed storage
- distributed-cache-backed storage
- device/session management storage

Do not expose refresh tokens to browser JavaScript.

## Data That Must Be Preserved Between Auth Steps

The app needs a server-side way to carry challenge state across pages.

Minimum challenge state shape:

- `PendingAuthenticationToken`
- `Kind`
- `Email`
- `EmailVerificationId`
- `AuthenticationChallengeId`
- `AuthenticationFactorId`
- `Organizations`
- `ReturnUrl`

Reasonable storage options:

- encrypted temp cookie
- protected server-side session
- distributed cache keyed by short nonce

Do not trust hidden fields alone for the full challenge payload.

## Security Guidance

- validate and sanitize return URLs
- use anti-forgery protection on POST forms
- store refresh tokens server-side only
- keep `ClientSecret` and `ApiKey` server-side only
- never embed WorkOS secrets in browser code
- use HTTPS in production
- if using custom auth domains, validate against the matching issuer
- log failures and challenge transitions without logging tokens or codes

## Claims And Authorization Guidance

After sign-in, the backend exposes normalized access context information for:

- subject id
- session id
- organization id
- roles
- permissions
- feature flags
- entitlements

The app should use:

- `ICurrentAccessContextAccessor` for current-request access context
- role/permission policy helpers for authorization checks

Do not read raw WorkOS claims everywhere in app code. Use the provider-neutral helpers.

## MVP Recommendations

If you want the smallest useful implementation on top of the current backend, build these first:

1. sign-in page with password form and redirect-provider buttons
2. callback endpoint
3. email verification page
4. MFA setup page
5. MFA verify page
6. organization selection page
7. sign-out endpoint
8. central auth-outcome router

Magic Auth can be phase 1 or phase 2 depending on product requirements.

## Full Current-Backend UI Recommendations

If you want to expose everything the current backend already supports, build:

1. sign-in page
2. magic auth request page
3. magic auth verify page
4. callback endpoint/page
5. email verification page
6. MFA setup page
7. MFA verify page
8. organization selection page
9. sign-out endpoint
10. auth error page
11. session expired page

## Full Product Recommendations Beyond Current Backend

If you want a complete authentication product comparable to hosted auth offerings, add backend support and pages for:

1. sign-up
2. forgot password
3. reset password
4. security settings
5. change password
6. session management
7. MFA reset

## Suggested Implementation Order For The UI Team

1. Wire dependency injection with `AddWorkOsCustomUiAuthentication(...)`
2. Implement outcome-routing helper
3. Build sign-in page and password flow
4. Build callback endpoint for redirect flows
5. Build email verification page
6. Build MFA setup and MFA verify pages
7. Build organization selection page
8. Add Magic Auth request/verify pages
9. Add sign-out endpoint and signed-out landing
10. Add auth error and session expired pages
11. Decide whether sign-up and password reset are in scope
12. If yes, add the missing backend APIs and then build those pages

## Testing Checklist For The App Team

### Required

- password sign-in success
- password sign-in failure
- password sign-in with email verification challenge
- password sign-in with MFA enrollment challenge
- password sign-in with MFA challenge
- password sign-in with org selection challenge
- redirect flow success
- Magic Auth request and verify
- sign-out clears local session
- sign-out revokes remote session when session id exists
- refresh works after cookie/session restoration

### Recommended

- invalid or expired code handling
- callback state mismatch
- invalid return URL rejection
- multi-org user selection persistence
- session-expired recovery flow

## Clear Product Decision Questions

Before implementation starts, the product owner should answer:

- do users sign up themselves, or are they provisioned/invited elsewhere?
- is Magic Auth enabled, or is email/password enough?
- are Google, Microsoft, or enterprise SSO buttons required?
- is MFA required for all users, some orgs, or nobody at launch?
- should WorkOS send auth emails, or should the app own delivery?
- do users need password reset at launch?
- do users need account security/settings at launch?

## Recommended Decision For A First Shipping Version

If the goal is to ship quickly using the backend already present here:

- support existing-user sign-in only
- build password sign-in
- optionally add redirect-provider buttons
- optionally add Magic Auth if product wants passwordless login
- implement all challenge pages
- defer self-service sign-up and password reset unless clearly required

## References

WorkOS docs used to shape this guidance:

- Authentication API: https://workos.com/docs/reference/user-management/authentication
- Sessions: https://workos.com/docs/user-management/sessions/introduction
- Session tokens: https://workos.com/docs/reference/user-management/session-tokens
- Magic Auth: https://workos.com/docs/reference/user-management/magic-auth/get
- Email verification: https://workos.com/docs/user-management/email-verification
- Password reset: https://workos.com/docs/reference/user-management/password-reset
- Custom auth domains: https://workos.com/docs/custom-domains/auth-api
- MFA: https://workos.com/docs/user-management/mfa
- Redirect URIs: https://workos.com/docs/sso/redirect-uris
- .NET SDK: https://workos.com/docs/sdks/dotnet
